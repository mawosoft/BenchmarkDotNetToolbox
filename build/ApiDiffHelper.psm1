# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Helpers for using the dotnet api-tools, particularly 'api-tools nuget-diff'.
#>

#Requires -Version 7

# Don't rely on these usings for OutputType and parameter type declarations!
using namespace System.IO
using namespace System.Collections.Generic
using namespace Microsoft.PackageManagement.Packaging

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

if ($null -eq (Get-ChildItem function:Start-NativeExecution -ErrorAction SilentlyContinue)) {
    . "$PSScriptRoot/startNativeExecution.ps1"
}

# .SYNOPSIS
#   Installs api-tools as dotnet global tool if it's not already installed.
function Install-ApiTools {
    [CmdletBinding()]
    param ()
    if ($null -eq (Get-Command api-tools -ErrorAction SilentlyContinue)) {
        # Ensure we are not leaking exec output to the pipeline because this might
        # have been called internally.
        Start-NativeExecution { dotnet tool install -g api-tools } -VerboseOutputOnError
    }
}

# .SYNOPSIS
# Adds NuGet.Versioning, installs the package if necessary.
function Add-NuGetVersioning {
    [CmdletBinding()]
    param (
        [ValidateSet('CurrentUser', 'AllUsers')]
        [string]$Scope = 'CurrentUser'
    )

    # Do nothing if type already exists.
    # Note: Cannot use [Type]::GetType() here, it needs the AssemblyQualifiedName.
    try { $null = [NuGet.Versioning.NuGetVersion]; return } catch {}

    [SoftwareIdentity]$pkg = Get-Package -Name NuGet.Versioning -ErrorAction SilentlyContinue
    if ($null -eq $pkg) {
        # W/o RequiredVersion this would take forever
        $null = Install-Package -Name NuGet.Versioning -RequiredVersion 6.1.0 `
            -Source https://api.nuget.org/v3/index.json -ProviderName NuGet -Scope $Scope `
            -Force
        # We can't use the return value from Install-Package because the source doesn't point
        # to the local package file.
        $pkg = Get-Package -Name NuGet.Versioning
    }
    Add-Type -Path (Join-Path ([Path]::GetDirectoryName($pkg.Source)) `
            'lib/netstandard2.0/NuGet.Versioning.dll')
}

# .SYNOPSIS
#   Convert the output from 'api-tools nuget-diff'
# .DESCRIPTION
#   Processes the output directory from 'api-tools nuget-diff', combines identical results,
#   and removes non-breaking and version-only diffs.
# .INPUTS
#   System.String. Directory paths.
# .OUTPUTS
#   [PSCustomObject]@{
#       DiffDirectory = 'full path'
#       Reference     = [PSCustomObject]@{ # from InvokeNuGetDiff.json
#           Name    = 'package name'
#           Version = 'package version'
#       }
#       Difference    = [PSCustomObject]@{ # from InvokeNuGetDiff.json
#           Name    = 'package name'
#           Version = 'package version'
#       }
#       AssemblyDiffs = @(
#           [PSCustomObject]@{
#               Name       = 'assembly.dll'
#               Frameworks = @('net5.0', 'other tfms with identical diffs')
#               Breaking   = 'content of markdown file with breaking changes'
#               Diff       = 'content of markdown file with all changes'
#           },
#           ...
#       )
#   }
function ConvertFrom-NuGetDiff {
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param (
        # Path of the api-tools output directory
        [Parameter(Mandatory, Position = 0, ValueFromPipeline)]
        [string]$Path,

        # Include non-breaking diffs. By default, these are removed.
        [Alias('nb', 'NonBreaking')]
        [switch]$IncludeNonBreaking,
        # Include diffs where only the assembly version has changed. By default, these are removed.
        [Alias('avc', 'VersionChange')]
        [switch]$IncludeAssemblyVersionChange,
        # Delete the diff directory after processing its content
        [Alias('del', 'Delete')]
        [switch]$DeleteDiffDirectory
    )

    process {
        # Paths usually are diffroot/tfm/assembly.dll.<breaking|diff>.md
        # But it *could* be that tfm is multiple levels, e.g. tfm/platform, so we treat anything
        # between diffroot and assembly as tfm.
        # Also note that api-tools nuget-diff seems to use only the lib folder of the package.
        # If the assemblies are in another folder like tools or runtimes, you have to extract them
        # yourself and use api-tools api-diff on each one individually.
        #
        # See:
        # https://github.com/mattleibow/Mono.ApiTools.NuGetDiff/blob/master/api-tools/NuGetDiffCommand.cs
        # https://github.com/mono/mono/blob/main/mcs/tools/mono-api-html/MarkdownFormatter.cs
        # https://github.com/mono/mono/blob/main/mcs/tools/mono-api-html/Formatter.cs
        $mdfiles = Get-ChildItem $Path -Filter *.md -Recurse | ForEach-Object {
            if (-not ($_.Name -cmatch '^(?<name>.*)\.(?<type>breaking|diff)\.md$')) {
                throw "Unknown diff type: $($_.FullName)"
            }
            [PSCustomObject]@{
                Name       = $Matches.name
                Type       = $Matches.type
                Frameworks = @([Path]::GetRelativePath($Path, $_.DirectoryName))
                Body       = Get-Content $_ -Raw
            }
        }

        # Combine identical diffs for multiple frameworks
        $mdfiles = $mdfiles | Group-Object -Property Name, Type, Body
        $mdfiles = $mdfiles | ForEach-Object {
            if ($_.Group.Count -gt 1) {
                $_.Group[0].Frameworks = $_.Group.Frameworks | Sort-Object
                $_.Group[0]
            }
            else {
                $_.Group[0]
            }
        }

        # Filter results containing only non-breaking or version-only changes, unless params say otherwise.
        $mdfiles = $mdfiles | Group-Object -Property Name, Frameworks | ForEach-Object {
            [string]$breaking = ($_.Group | Where-Object Type -EQ 'breaking' | Select-Object -First 1)?.Body
            [string]$diff = ($_.Group | Where-Object Type -EQ 'diff' | Select-Object -First 1).Body
            if (-not $IncludeNonBreaking.IsPresent) {
                if ($null -eq $breaking) { return }
                if ($_.Group.Count -gt 1) {
                    if ($diff -cmatch '[\r\n]> No changes\.[\r\n]') { return }
                    if (-not $IncludeAssemblyVersionChange.IsPresent -and $breaking -cnotmatch '[\r\n]###') {
                        [string]$tmp = $breaking -creplace `
                            '[\r\n]> Assembly Version Changed: (\d+\.){3}\d+ vs (\d+\.){3}\d+', ''
                        if ($tmp -cmatch '[\r\n]##[^\r\n]+[\r\n]+(.*)' -and $Matches[1].Trim().Length -eq 0) {
                            return
                        }
                    }
                }
            }
            [PSCustomObject]@{
                Name       = $_.Group[0].Name
                Frameworks = $_.Group[0].Frameworks
                Breaking   = $breaking
                Diff       = $diff
            }
        }

        # Make sure single results remain as array and wrap into single object with meta infos.
        if ($null -eq $mdfiles) { $mdfiles = @() }
        elseif ($mdfiles -isnot [Array]) { $mdfiles = @($mdfiles) }

        # Get additional info provided by Invoke-NuGetDiff
        $info = $null
        [string]$infofile = Join-Path $Path 'InvokeNuGetDiff.json'
        if (Test-Path $infofile -PathType Leaf) {
            $info = Get-Content $infofile -Raw | ConvertFrom-Json
        }

        [string]$diffdir = Convert-Path $Path
        if ($DeleteDiffDirectory.IsPresent) {
            Remove-Item $diffdir -Recurse -ErrorAction SilentlyContinue
            $diffdir = $null
        }

        return [PSCustomObject]@{
            DiffDirectory = $diffdir
            Reference     = ${info}?.Reference
            Difference    = ${info}?.Difference
            AssemblyDiffs = $mdfiles
        }
    }
}

# .SYNOPSIS
#   Invoke 'api-tools nuget-diff' on given NuGet packages
# .DESCRIPTION
#   Invokes 'api-tools nuget-diff', while handling temporary package downloads and diff directory creation.
# .INPUTS
#   Microsoft.PackageManagement.Packaging.SoftwareIdentity.
#   Using the pipeline enables incremental diffs.
# .OUTPUTS
#   System.IO.DirectoryInfo. The diff directory containing the results.
function Invoke-NuGetDiff {
    [CmdletBinding(DefaultParameterSetName = 'ReferenceDifference')]
    [OutputType([System.IO.DirectoryInfo])]
    param (
        # The reference package (lower version) as SoftwareIdentity, obtained via Find-Package.
        [Parameter(Mandatory, ValueFromPipelineByPropertyName, Position = 0,
            ParameterSetName = 'ReferenceDifference')]
        [Microsoft.PackageManagement.Packaging.SoftwareIdentity]$ReferencePackage,

        # The difference package (higher version) as SoftwareIdentity, obtained via Find-Package.
        [Parameter(Mandatory, ValueFromPipelineByPropertyName, Position = 1,
            ParameterSetName = 'ReferenceDifference')]
        [Microsoft.PackageManagement.Packaging.SoftwareIdentity]$DifferencePackage,

        [Parameter(ValueFromPipeline, ParameterSetName = 'InputObject')]
        [Microsoft.PackageManagement.Packaging.SoftwareIdentity]$InputObject
    )
    begin {
        # Note: During the begin block, the ParameterSetName has not yet been selected, meaning
        # we have to move init code that depends on it into the process block.
        Install-ApiTools
        [string]$packageDirectory = Join-Path ([Path]::GetTempPath()) ([Path]::GetRandomFileName())
        $null = [Directory]::CreateDirectory($packageDirectory)
        [SoftwareIdentity]$previousPackage = $null
        [Queue[string]]$removalQueue = [Queue[string]]::new()

        # Get or download the requested package
        function GetLocalPackage {
            [OutputType([SoftwareIdentity])]
            param (
                [SoftwareIdentity]$package
            )
            if (Test-Path $package.Source -PathType Leaf) {
                return $package
            }
            [SoftwareIdentity]$localPackage = Get-Package -Name $package.Name -RequiredVersion $package.Version `
                -Destination $packageDirectory -ErrorAction SilentlyContinue
            if ($null -ne $localPackage) {
                return $localPackage
            }
            $localPackage = Install-Package $package -SkipDependencies -Force -Destination $packageDirectory
            $localPackage = Get-Package -Name $localPackage.Name -RequiredVersion $localPackage.Version `
                -Destination $packageDirectory
            # Only queue packages for removal that we have installed here
            $removalQueue.Enqueue([Path]::GetDirectoryName($localPackage.Source))
            return $localPackage
        }

        function CompareLocalPackage {
            [OutputType([DirectoryInfo])]
            param (
                [SoftwareIdentity]$refPackage,
                [SoftwareIdentity]$diffPackage
            )
            [string]$diffDirectory = Join-Path ([Path]::GetTempPath()) ([Path]::GetRandomFileName())
            Start-NativeExecution {
                api-tools nuget-diff $refPackage.Source $diffPackage.Source --output $diffDirectory
            } -VerboseOutputOnError
            [string]$infofile = Join-Path $diffDirectory 'InvokeNuGetDiff.json'
            $null = [PSCustomObject]@{
                Reference  = [PSCustomObject]@{
                    Name    = $refPackage.Name
                    Version = $refPackage.Version
                }
                Difference = [PSCustomObject]@{
                    Name    = $diffPackage.Name
                    Version = $diffPackage.Version
                }
            } | ConvertTo-Json | New-Item -Path $infofile -ItemType File
            return Get-Item -LiteralPath $diffDirectory
        }
    }
    process {
        [DirectoryInfo]$retVal = $null
        if ($PSCmdlet.ParameterSetName -eq 'ReferenceDifference') {
            $retVal = CompareLocalPackage `
                -refPackage (GetLocalPackage $ReferencePackage $packageDirectory) `
                -diffPackage (GetLocalPackage $DifferencePackage $packageDirectory)
        }
        else {
            Add-NuGetVersioning
            [SoftwareIdentity]$inputPackage = GetLocalPackage $InputObject $packageDirectory
            if ($null -ne $previousPackage) {
                if ([NuGet.Versioning.NuGetVersion]$inputPackage.Version `
                        -lt [NuGet.Versioning.NuGetVersion]$previousPackage.Version) {
                    $retVal = CompareLocalPackage `
                        -refPackage (GetLocalPackage $inputPackage $packageDirectory) `
                        -diffPackage (GetLocalPackage $previousPackage $packageDirectory)
                }
                else {
                    $retVal = CompareLocalPackage `
                        -refPackage (GetLocalPackage $previousPackage $packageDirectory) `
                        -diffPackage (GetLocalPackage $inputPackage $packageDirectory)
                }
            }
            $previousPackage = $inputPackage
        }

        while ($removalQueue.Count -gt 2) {
            Remove-Item -LiteralPath $removalQueue.Dequeue() -Recurse -ErrorAction SilentlyContinue
        }
        if ($null -ne $retval) {
            return $retVal
        }
    }
    end {
        Remove-Item -LiteralPath $packageDirectory -Recurse -ErrorAction SilentlyContinue
    }
}

Export-ModuleMember -Function @(
    'Install-ApiTools',
    'Add-NuGetVersioning',
    'ConvertFrom-NuGetDiff',
    'Invoke-NuGetDiff'
)
