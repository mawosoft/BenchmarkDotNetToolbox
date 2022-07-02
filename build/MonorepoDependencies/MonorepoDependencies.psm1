# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Manages dependencies on packages produced by Monorepos.
.DESCRIPTION
    Manages dependencies on packages produced by Monorepos without individual versioning by
    tracking changes to the relevant paths in the repo.
.NOTES
    The classes in this script module depend on external types (NuGet.*) added via ScriptsToProcess.
    Due to various bugs in PowerShell (https://github.com/PowerShell/PowerShell/issues/6652),
    the 'using module' statement has to be invoked AFTER importing the module, e.g.

      Import-Module "$PSScriptRoot/MonorepoDependencies.psd1"
      Invoke-Expression "using module $PSScriptRoot/MonorepoDependencies.psd1"

#>

#Requires -Version 7

using namespace System
using namespace System.IO
using namespace System.Collections.Generic
using namespace System.Reflection
using namespace System.Xml
using namespace System.Threading
using namespace System.Threading.Tasks
using namespace Microsoft.PowerShell.Commands
using namespace NuGet.Common
using namespace NuGet.Core
using namespace NuGet.Packaging
using namespace NuGet.Packaging.Core
using namespace NuGet.Protocol
using namespace NuGet.Protocol.Core.Types
using namespace NuGet.Versioning

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

. "$PSScriptRoot/../startNativeExecution.ps1"

# .SYNOPSIS
#   Version info for a Monorepo package
# .NOTES
#   Properties can be set explicit, or via some automated means.
class MonorepoPackageVersion {
    # Package version. Auto: From a manifest or project file.
    [ValidateNotNullOrEmpty()][string]$Version
    # Associated Commit. Auto: from Repository metadata in the Nuspec file.
    [string]$Commit
    # How many commits ahead/behind of the relevant repo paths.
    [int]$AheadOfPathsCount

    # Default ctors needed for all types to assign from [PSCustomObject]/ConvertFrom-Json.
    MonorepoPackageVersion() { }
    MonorepoPackageVersion([string]$version) { $this.Version = $version }
}

# .SYNOPSIS
#   Path info for the source code of a Monorepo package
class MonorepoPackagePath : ICloneable {
    # Path relative to repo root
    [ValidateNotNullOrEmpty()][string]$Path
    # Previously checked commit within this path
    [string]$CheckedCommit
    # Latest commit within this path
    [string]$LatestCommit
    # How many commits ahead compared to previously checked commit.
    [int]$LatestAheadCount
    # Git log for commit range
    [string[]]$LatestAheadLog

    MonorepoPackagePath() { }
    MonorepoPackagePath([string]$path) { $this.Path = $path }

    [Object]Clone() {
        [MonorepoPackagePath]$clone = [MonorepoPackagePath]::new()
        $clone.Path = $this.Path
        $clone.CheckedCommit = $this.CheckedCommit
        $clone.LatestCommit = $this.LatestCommit
        $clone.LatestAheadCount = $this.LatestAheadCount
        $clone.LatestAheadLog = $this.LatestAheadLog?.psobject?.Copy()
        return $clone
    }

    # Copy properties to target objects that have the same path
    [void]CopyTo([MonorepoPackagePath[]]$targets) {
        foreach ($target in $targets) {
            if (-not $this.Path.Equals($target.Path)) {
                throw [InvalidOperationException]::new('CopyTo() target path differs.')
            }
            $target.CheckedCommit = $this.CheckedCommit
            $target.LatestCommit = $this.LatestCommit
            $target.LatestAheadCount = $this.LatestAheadCount
            $target.LatestAheadLog = $this.LatestAheadLog
        }
    }

    # Uses the latest commit as previously checked one and resets the latest commit properties.
    [void]BeforeUpdateLatest() {
        $this.CheckedCommit = $this.LatestCommit
        $this.LatestAheadCount = 0
        $this.LatestAheadLog = $null
    }

    # Update latest commit from repo
    [void]UpdateLatestCommit([GitRepoHelper]$gitRepo) {
        $this.LatestAheadLog = $null
        $this.LatestCommit = $gitRepo.GetLatestCommit($this.Path)
        $this.LatestAheadCount = $gitRepo.CountRange($this.CheckedCommit, $this.LatestCommit, $this.Path)
        if ($this.LatestAheadCount -notin 0, [int]::MaxValue) {
            if ($this.LatestAheadCount -eq [int]::MinValue) {
                $this.LatestAheadLog = $this.CheckedCommit + ' Path no longer exists'
            }
            else {
                $this.LatestAheadLog = $gitRepo.LogRange($this.CheckedCommit, $this.LatestCommit, $this.Path)
            }
        }
    }
}

# .SYNOPSIS
#   Defines a package from a monorepo
class MonorepoPackage : ICloneable {
    # Package name
    [ValidateNotNullOrEmpty()][string]$Name
    # Package source feed
    [ValidateNotNullOrEmpty()][string]$Feed
    # Package version in use
    [MonorepoPackageVersion]$Version
    # Git log for commit range if $Version is outdated
    [string[]]$VersionBehindLog
    # Latest package version previously checked
    [MonorepoPackageVersion]$CheckedVersion
    # Latest available package version on the feed
    [MonorepoPackageVersion]$LatestVersion
    # Path of a tool manifest file or a project file that defines the package version in use
    [string]$VersionSource
    # Git repository containing the package source code
    [ValidateNotNullOrEmpty()][string]$Repo
    # Paths in the repo containing the package source code
    [ValidateNotNullOrEmpty()][MonorepoPackagePath[]]$Paths

    MonorepoPackage() { }
    MonorepoPackage([string]$name, [string]$feed, [string]$versionSource, [string]$repo, [string[]]$paths) {
        $this.Name = $name
        $this.Feed = $feed
        $this.VersionSource = $versionSource
        $this.Repo = $repo
        $this.Paths = $paths
    }

    [Object]Clone() {
        [MonorepoPackage]$clone = [MonorepoPackage]::new()
        $clone.Name = $this.Name
        $clone.Feed = $this.Feed
        $clone.Version = $this.Version?.psobject?.Copy()
        $clone.VersionBehindLog = $this.VersionBehindLog?.psobject?.Copy()
        $clone.CheckedVersion = $this.CheckedVersion?.psobject?.Copy()
        $clone.LatestVersion = $this.LatestVersion?.psobject?.Copy()
        $clone.VersionSource = $this.VersionSource
        $clone.Repo = $this.Repo
        $clone.Paths = $this.Paths?.psobject?.Copy()
        return $clone
    }

    [void]BeforeUpdateLatest() {
        $this.CheckedVersion = $this.LatestVersion?.psobject?.Copy()
        $this.Paths.BeforeUpdateLatest()
    }

    # Update version infos from repo and (if needed) from feed.
    # Expects latest Paths commit info to be already updated.
    # Returns $true for successful update, or $false if feed is needed, but wasn't provided.
    [bool]UpdateVersions([GitRepoHelper]$gitRepo, [FindPackageByIdResource]$finder, [SourceCacheContext]$cache,
        [NuGet.Common.ILogger]$logger
    ) {
        $logger.LogVerbose('Updating package: ' + $this.Name)
        $this.VersionBehindLog = $null
        if ($this.Version?.Version) {
            if (-not $this.Version.Commit) {
                if (-not $finder) { return $false }
                $this.Version = $this.GetVersionFromFeed($this.Version.Version, $finder, $cache, $logger)
            }
            $this.Version.AheadOfPathsCount = $gitRepo.CountRange('HEAD', $this.Version.Commit, $this.Paths.Path)
            if ($this.Version.AheadOfPathsCount -lt 0 -and $this.Version.AheadOfPathsCount -ne [int]::MinValue) {
                $this.VersionBehindLog = $gitRepo.LogRange('HEAD', $this.Version.Commit, $this.Paths.Path)
            }
        }
        if (-not $this.CheckedVersion?.Version -or
            -not $this.CheckedVersion.Commit -or
            $this.CheckedVersion.AheadOfPathsCount -lt 0) {
            if (-not $finder) { return $false }
            $this.LatestVersion = $this.GetVersionFromFeed($null, $finder, $cache, $logger)
        }
        $this.LatestVersion.AheadOfPathsCount = $gitRepo.CountRange('HEAD', $this.LatestVersion.Commit, $this.Paths.Path)
        return $true
    }

    # Returns version and commit from the feed.
    # To get the latest version, pass $null for $requestedVersion
    hidden [MonorepoPackageVersion]GetVersionFromFeed(
        [string]$requestedVersion, [FindPackageByIdResource]$finder, [SourceCacheContext]$cache,
        [NuGet.Common.ILogger]$logger
    ) {
        if (-not $requestedVersion) {
            $requestedVersion = ($finder.GetAllVersionsAsync($this.Name, $cache, $logger,
                    [CancellationToken]::None).GetAwaiter().GetResult() | Sort-Object -Descending)[0]
        }
        [PackageArchiveReader]$packageReader = $null
        [FileStream]$stream = $null
        [FileInfo]$tmpfile = New-TemporaryFile
        try {
            $stream = $tmpfile.Open([FileMode]::OpenOrCreate)
            [bool]$ok = $finder.CopyNupkgToStreamAsync($this.Name, $requestedVersion, $stream, $cache,
                $logger, [CancellationToken]::None).GetAwaiter().GetResult()
            if (-not $ok) {
                throw "CopyNupkgToStreamAsync failed for $($this.Name) $requestedVersion"
                return $null
            }
            $packageReader = [PackageArchiveReader]::new($stream)
            [NuspecReader] $nuspecReader = $packageReader.GetNuspecReaderAsync(
                [CancellationToken]::None).GetAwaiter().GetResult()
            [RepositoryMetadata]$meta = $nuspecReader.GetRepositoryMetadata()
            [MonorepoPackageVersion]$retVal = [MonorepoPackageVersion]::new($requestedVersion)
            $retVal.Commit = $meta.Commit ? $meta.Commit : [GitRepoHelper]::NoCommitSha
            return $retVal

        }
        finally {
            ${packageReader}?.Dispose()
            ${stream}?.Dispose()
            $tmpfile.Delete()
        }
    }
}

# .SYNOPSIS
#   Manages dependencies on packages produced by Monorepos.
class MonorepoDependencies : ICloneable {
    # Logger. Defaults to [WriteHostLogger]::Instance. Alternative: [NullLogger]::Instance
    # Caller should set VerbosityLevel.
    [NuGet.Common.ILogger]$Logger = [WriteHostLogger]::Instance
    # GiHub OAuth token. Only used for REST API.
    [securestring]$GitHubToken
    # Package infos
    [ValidateNotNullOrEmpty()][MonorepoPackage[]] $Packages

    MonorepoDependencies() { }
    MonorepoDependencies([MonorepoPackage[]] $packages) {
        $this.Packages = ${packages}?.psobject?.Copy()
    }

    [Object]Clone() {
        [MonorepoDependencies]$clone = [MonorepoDependencies]::new()
        $clone.Logger = $this.Logger
        $clone.Packages = $this.Packages?.psobject?.Copy()
        return $clone
    }

    # Convert to JSON
    [string]ToJson() {
        # Pwsh uses [Newtonsoft.Json]. Alternative: [System.Text.Json.JsonSerializer]
        return $this.Packages | ConvertTo-Json -Depth 4
    }

    # Update existing packages with transient data from JSON.
    # To restore all data from JSON, create the $Packages property directly.
    [void]UpdateFromJson([string]$jsonText) {
        if (-not $this.Packages) {
            $this.Logger.LogWarning('UpdateFromJson: No predefined packages')
            $this.Packages = $jsonText | ConvertFrom-Json
        }
        else {
            [List[MonorepoPackage]]$jsonPackages = $jsonText | ConvertFrom-Json
            foreach ($package in $this.Packages) {
                [MonorepoPackage]$patch = $jsonPackages.Find({ param([MonorepoPackage]$p) $p.Name -eq $package.Name })
                if ($patch) {
                    if ($package.Version?.Version -eq $patch.Version?.Version) {
                        $package.Version = $patch.Version
                        $package.VersionBehindLog = $patch.VersionBehindLog
                    }
                    $package.LatestVersion = $patch.LatestVersion
                    [List[MonorepoPackagePath]]$patchPaths = $patch.Paths
                    for ([int]$i = 0; $i -lt $package.Paths.Count; $i++) {
                        [string]$path = $package.Paths[$i].Path
                        [MonorepoPackagePath]$patchPath = $patchPaths.Find({ param([MonorepoPackagePath]$p) $p.Path -eq $path })
                        if ($patchPath) {
                            $package.Paths[$i] = $patchPath
                        }
                    }
                }
            }
        }
    }

    # Updates the packages with versions from tool manifest or project files
    [void]UpdateFromManifestOrProjectFile() {
        $this.Packages | Where-Object -Property VersionSource | Group-Object -Property VersionSource |
        ForEach-Object {
            [hashtable]$pkgversions = @{}
            if ($_.Values[0].EndsWith('.json', [StringComparison]::OrdinalIgnoreCase)) {
                $manifest = Get-Content $_.Values[0] -Raw | ConvertFrom-Json
                $manifest.tools.psobject.Properties.ForEach({ $pkgversions.Add($_.Name, $_.Value.version) })
            }
            else {
                # Expects version(range) literals and ignores any conditionals, but prioritizes Update over Include.
                [XmlNode]$project = (Select-Xml -Path $_.Values[0] -XPath '/*').Node
                [XmlNode[]]$pkgrefs = (Select-Xml -Xml $project -Namespace @{ ns = $project.NamespaceURI } `
                        -XPath '(//ns:PackageReference|//ns:PackageVersion|//ns:PackageDownload)[@ns:Version|ns:Version]')?.Node
                $pkgrefs | Where-Object { $_.GetAttribute('Include') } | ForEach-Object {
                    $pkgversions[$_.Include] = $_.Version
                }
                $pkgrefs | Where-Object { $_.GetAttribute('Update') } | ForEach-Object {
                    $pkgversions[$_.Update] = $_.Version
                }
            }
            $_.Group | ForEach-Object {
                [VersionRange]$vr = $pkgversions[$_.Name]
                if ($vr) {
                    [string]$version = $vr.FindBestMatch([NuGetVersion[]]@($vr.MinVersion, $vr.MaxVersion))
                    # Try not to lose commit info
                    if ($version -ne $_.Version?.Version) {
                        $_.VersionBehindLog = $null
                        if ($version -eq $_.CheckedVersion?.Version) {
                            $_.Version = $_.CheckedVersion.psobject.Copy()
                        }
                        elseif ($version -eq $_.LatestVersion?.Version) {
                            $_.Version = $_.LatestVersion.psobject.Copy()
                        }
                        else {
                            $_.Version = [MonorepoPackageVersion]::new($version)
                        }
                    }
                }
            }
        }
    }

    # Update all packages with latest info from repo and (if needed) feed
    [void]UpdateLatest() {
        $this.Packages.BeforeUpdateLatest()
        $this.Packages | Group-Object -Property Repo | ForEach-Object {
            $this.Logger.LogVerbose('Repo: ' + $_.Values[0])
            if ($this.QuickUpdateUnchanged($_.Group)) {
                $this.Logger.LogVerbose('Quick check succeeded. Cloning skipped.')
            }
            else {
                [GitRepoHelper]$gitRepo = [GitRepoHelper]::new($_.Values[0], $null)
                try {
                    $gitRepo.CloneNoCheckout()
                    $this.Logger.LogVerbose('Cloned into: ' + $gitRepo.LocalRepo)
                    # The same paths may appear in multiple packages.
                    $_.Group.Paths |
                    Group-Object -Property Path -CaseSensitive `
                        -Culture ([System.Globalization.CultureInfo]::InvariantCulture.LCID) |
                    ForEach-Object {
                        [MonorepoPackagePath]$source = $_.Group[0]
                        $this.Logger.LogVerbose('Updating path: ' + $source.Path)
                        # Update path once and copy info to the others
                        $source.UpdateLatestCommit($gitRepo)
                        $source.CopyTo($_.Group)
                    }
                    $_.Group | Group-Object -Property Feed | ForEach-Object {
                        [SourceCacheContext]$cache = $null
                        [FindPackageByIdResource]$finder = $null
                        try {
                            $_.Group | ForEach-Object {
                                if ($finder) {
                                    $null = $_.UpdateVersions($gitRepo, $finder, $cache, $this.Logger)
                                }
                                elseif (-not $_.UpdateVersions($gitRepo, $null, $null, $this.Logger)) {
                                    $cache = [SourceCacheContext]::new()
                                    [SourceRepository]$feed = [FactoryExtensionsV3]::GetCoreV3([Repository]::Factory, $_.Feed)
                                    [MethodInfo]$mi = [SourceRepository].GetMethod('GetResourceAsync', [type]::EmptyTypes)
                                    [MethodInfo]$getResourceAsync = $mi.MakeGenericMethod([FindPackageByIdResource])
                                    $finder = $getResourceAsync.Invoke($feed, @()).GetAwaiter().GetResult()
                                    $null = $_.UpdateVersions($gitRepo, $finder, $cache, $this.Logger)
                                }
                            }
                        }
                        finally {
                            ${cache}?.Dispose()
                        }
                    }
                }
                finally {
                    $gitRepo.Dispose()
                }
            }
        }
    }

    # Quickly check for unchanged via GitHub REST API.
    # Returns $true on success, or $false if not possible or any error occured.
    # Caller is responsible for grouping packages by Repo
    hidden [bool]QuickUpdateUnchanged([MonorepoPackage[]]$packages) {
        if (-not $packages) { return $false }
        [string]$github = 'https://github.com/'
        if (-not $packages[0].Repo.StartsWith($github, [StringComparison]::OrdinalIgnoreCase)) {
            return $false
        }
        if ($packages.Where({
                    -not $_.Version?.Commit -or -not $_.LatestVersion?.Commit -or -not ($_.LatestVersion?.AheadOfPathsCount -ge 0)
                }, 'First')) { return $false }
        if ($packages.Paths.Where({ -not $_.CheckedCommit }, 'First')) { return $false }
        [string]$uri = "https://api.github.com/repos/$($packages[0].Repo.Substring($github.Length).TrimEnd('/'))/commits?per_page=1&path="
        [hashtable]$auth = @{}
        if ($this.GitHubToken) {
            $auth.Authentication = 'Bearer'
            $auth.Token = $this.GitHubToken
        }
        try {
            foreach ($pathGroup in ($packages.Paths |
                    Group-Object -Property Path -CaseSensitive `
                        -Culture ([System.Globalization.CultureInfo]::InvariantCulture.LCID))) {
                [MonorepoPackagePath]$source = $pathGroup.Group[0]
                $result = Invoke-RestMethod -Uri ($uri + [System.Net.WebUtility]::UrlEncode($source.Path)) @auth
                if (-not $result -or $result[0].sha -ne $source.CheckedCommit) { return $false }
                $source.LatestCommit = $result[0].sha
                $source.LatestAheadCount = 0
                $source.LatestAheadLog = $null
                $source.CopyTo($pathGroup.Group)
            }
            return $true
        }
        catch {
            $this.Logger.LogVerbose('Something went wrong in QuickUpdateUnchanged():')
            $this.Logger.LogVerbose($_)
            return $false
        }
    }

    # Returns updated paths per repo as a dictionary.
    [KeyValuePair[string, MonorepoPackagePath[]][]]GetUpdatedPaths() {
        [List[KeyValuePair[string, MonorepoPackagePath[]]]]$repoPaths = @()
        $this.Packages | Group-Object -Property Repo | ForEach-Object {
            [MonorepoPackagePath[]]$paths = $_.Group.Paths | Where-Object -Property LatestAheadLog |
            Sort-Object -Property Path -Unique -CaseSensitive `
                -Culture ([System.Globalization.CultureInfo]::InvariantCulture.LCID)
            if ($paths) { $repoPaths.Add([KeyValuePair]::Create($_.Values[0], $paths)) }
        }
        return $repoPaths;
    }

    # Returns newly outdated packages
    [MonorepoPackage[]]GetNewOutdatedPackages() {
        return $this.Packages.Where({
                $_.VersionBehindLog -and 
                ($_.Paths.LatestAheadCount -gt 0 -or 
                ($_.CheckedVersion -and $_.CheckedVersion.AheadOfPathsCount -lt 0 -and 
                $_.LatestVersion -and $_.LatestVersion.AheadOfPathsCount -ge 0))
            }) | Sort-Object Name
    }

    # Returns all outdated packages
    [MonorepoPackage[]]GetAllOutdatedPackages() {
        return $this.Packages | Where-Object VersionBehindLog | Sort-Object Name
    }
}

################################################################################
#   Helper classes

# .SYNOPSIS
#   Helper class for some Git commands
class GitRepoHelper : IDisposable {
    # Magic for non-existing commit, e.g. if not available for a package version
    static [string]$NoCommitSha = '<nocommit>'
    # Repo Uri
    [ValidateNotNullOrEmpty()][string]$Repo
    # Repo local directory
    [ValidateNotNullOrEmpty()][string]$LocalRepo
    hidden [bool]$_shouldDispose
    hidden [bool]$_isDisposed

    # Creates a GitRepoHelper.
    # To use a disposable temporary local repo directory, pass $null for $localRepo.
    GitRepoHelper([string]$repo, [string]$localRepo) {
        $this.Repo = $repo
        if ($localRepo) {
            $this.LocalRepo = $localRepo
        }
        else {
            $this.LocalRepo = Join-Path ([Path]::GetTempPath()) ([Path]::GetRandomFileName())
            $null = New-Item $this.LocalRepo -ItemType 'Directory' -Force
            $this._shouldDispose = $true
        }
    }

    # Clones the repo without checking it out.
    [void]CloneNoCheckout() {
        $this.CheckDisposed()
        Start-NativeExecution { git clone $this.Repo $this.LocalRepo --no-checkout } -VerboseOutputOnError
    }

    # Returns a brief log for the given commit range, respecting paths.
    [string[]]LogRange([string]$commit1, [string]$commit2, [string[]]$paths) {
        $this.CheckDisposed()
        return Start-NativeExecution { git -C $this.LocalRepo rev-list "$commit1...$commit2" '--format=%h %s (%as)' --no-commit-header -- $paths }
    }

    # Returns the latest commit with regards to paths.
    [string]GetLatestCommit([string[]]$paths) {
        $this.CheckDisposed()
        return Start-NativeExecution { git -C $this.LocalRepo rev-list HEAD -n 1 -- $paths }
    }

    # Returns the number of commits in the given range, respecting paths.
    # A negative number indicates that $differenceCommit is behind $referenceCommit.
    # Special values such as $null or [GitRepoHelper]::NoCommitSha are allowed and return
    # [int]MinValue/MaxValue as result (if one-sided).
    [int]CountRange([string]$referenceCommit, [string]$differenceCommit, [string[]]$paths) {
        $this.CheckDisposed()
        if ($referenceCommit -eq $differenceCommit) {
            return 0 # Equal is most frequent
        }
        if (-not $referenceCommit -or $referenceCommit -eq [GitRepoHelper]::NoCommitSha) {
            if (-not $differenceCommit -or $differenceCommit -eq [GitRepoHelper]::NoCommitSha) {
                return 0
            }
            else {
                return ([int]::MaxValue)
            }
        }
        elseif (-not $differenceCommit -or $differenceCommit -eq [GitRepoHelper]::NoCommitSha) {
            return ([int]::MinValue)
        }
        [int]$count = Start-NativeExecution {
            git -C $this.LocalRepo rev-list "$referenceCommit..$differenceCommit" --count -- $paths
        }
        if ($count -ne 0) {
            return $count
        }
        $count = Start-NativeExecution {
            git -C $this.LocalRepo rev-list "$differenceCommit..$referenceCommit" --count -- $paths
        }
        return - $count
    }

    hidden[void]CheckDisposed() {
        if (-not $this._isDisposed) { return }
        throw [ObjectDisposedException]::new($this.GetType().Name)
    }

    [void]Dispose() {
        if (-not $this._shouldDispose -or $this._isDisposed) { return }
        # -Force because '.git' directory is hidden
        Remove-Item $this.LocalRepo -Recurse -Force -ErrorAction Ignore
        $this._isDisposed = $true
    }
}

# .SYNOPSIS
#   Write-Host logger
class WriteHostLogger : NuGet.Common.LoggerBase {
    static [WriteHostLogger]$Instance = [WriteHostLogger]::new()
    [void]Log([ILogMessage]$message) {
        Write-Host $message.Message
    }
    [Task]LogAsync([ILogMessage]$message) {
        Log($message)
        return [Task]::CompletedTask
    }
}
