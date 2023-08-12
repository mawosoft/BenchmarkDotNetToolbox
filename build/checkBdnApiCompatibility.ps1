# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Check latest nightly version of BenchmarkDotNet packages for breaking API changes.

.DESCRIPTION
    Check latest nightly version of BenchmarkDotNet packages for breaking API changes.
    Discovers new versions to check via artifacts from previous workflow runs. The script will
    download the previous artifact, but the caller is responsible for uploading the new ones.
    Creates an issue, if new breaking changes have been found.

.OUTPUTS
    None. Sets the following workflow step output parameters via workflow commands issued by Write-Host.
        StatusArtifactName  - Artifact name for the status file to share between workflow runs.
        StatusArtifactPath  - Artifact path for the status file.

        LogArtifactName     - Artifact name for the API compatibility log file(s) generated if the log size
                              is larger than the allowed issue body size (65536 chars).
                              Otherwise not set.
        LogArtifactPath     - Artifact path for log file(s) or not set.

        LastCheckedVersion  - Last checked BDN version.
        IssueNumber         - Issue with report of breaking changes.
        IsBreaking          - 'true' if a new breaking issue has been reported.

.NOTES
    TODO: Add multi-TFM support.
    - BenchmarkDotNet: netstandard2.0 + net6.0
    - BenchmarkDotNet.Annotations: netstandard2.0 + netstandard1.0
#>

#Requires -Version 7

using namespace System
using namespace System.IO
using namespace System.Collections.Generic
using namespace System.Text


[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [Alias('Token')]
    [securestring]$GitHubToken,

    [Alias('Previous')]
    [string]$PreviousVersionOverride,

    [switch]$FullCompare,

    [ValidateSet('Report', 'ReportAndSummary', 'Summary', 'None')]
    [string]$IssueType = 'Report',

    [Alias('Labels')]
    [string[]]$IssueLabels = @('BDN change'),

    [string]$BadgeUri = 'https://img.shields.io/badge/Test%20Run-pending-lightgrey',

    [ValidateNotNullOrEmpty()]
    [string]$StatusArtifactName = 'BdnApiCompatStatus',

    [ValidateNotNullOrEmpty()]
    [string]$LogArtifactName = 'BdnApiCompatLog'
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Import-Module "$PSScriptRoot/ApiCompat/ApiCompatHelper.psm1" -Force
Import-Module "$PSScriptRoot/GitHubHelper.psm1" -Force
. "$PSScriptRoot/startNativeExecution.ps1"
# Even if we would add [NuGet.Versioning] here right away, the types could not be used in classes.
. "$PSScriptRoot/addNuGetType.ps1"

# .SYNOPSIS
#   General BDN package descriptor
class BdnPackageInfo {
    [ValidateNotNullOrEmpty()][string]$Name
    [string[]]$RelativeAsmPaths
    BdnPackageInfo([string]$name) {
        $this.Name = $name
        $this.RelativeAsmPaths = , "lib/netstandard2.0/$name.dll"
    }
}

# .SYNOPSIS
#   Restores a set of BDN packages with the same version
class BdnPackageSet {
    static [string]$BaselineFeed = 'https://api.nuget.org/v3/index.json'
    static [string]$NightlyFeed = 'https://www.myget.org/F/benchmarkdotnet/api/v3/index.json'
    static [string]$BaselineVersion = '0.13.4'
    static [string]$DownloadProjectFilePath = (Join-Path $script:PSScriptRoot 'ApiCompat/BdnDownload/BdnDownload.proj')
    static [string]$NugetPackageRootPath = (Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages')
    static [ValidateNotNullOrEmpty()][BdnPackageInfo[]]$Infos = @(
        [BdnPackageInfo]::new('BenchmarkDotNet')
        [BdnPackageInfo]::new('BenchmarkDotNet.Annotations')
        [BdnPackageInfo]::new('BenchmarkDotNet.Diagnostics.Windows')
    )
    [ValidateNotNullOrEmpty()][string]$Name = 'BDN'
    [ValidateNotNullOrEmpty()][string]$Version
    [List[string]]$AssemblyFilePaths
    [List[string]]$AssemblyDirectoryPaths

    BdnPackageSet([string]$version) {
        $this.Version = $version
    }

    [bool]IsRestored() {
        if (-not $this.AssemblyFilePaths -or -not $this.AssemblyDirectoryPaths) {
            $this.AssemblyFilePaths = [List[string]]::new()
            $this.AssemblyDirectoryPaths = [List[string]]::new()
            foreach ($info in [BdnPackageSet]::Infos) {
                [string]$pkgdir = Join-Path ([BdnPackageSet]::NugetPackageRootPath) "$($info.Name.ToLowerInvariant())/$($this.Version.ToLowerInvariant())"
                foreach ($path in $info.RelativeAsmPaths) {
                    [string]$fullFilePath = Join-Path $pkgdir $path
                    $this.AssemblyFilePaths.Add($fullFilePath)
                    [string]$fullDirPath = Split-Path $fullFilePath -Parent
                    if ($this.AssemblyDirectoryPaths -notcontains $fullDirPath) {
                        $this.AssemblyDirectoryPaths.Add($fullDirPath)
                    }
                }
            }
        }
        foreach ($path in $this.AssemblyFilePaths) {
            if (-not (Test-Path $path)) { return $false }
        }
        return $true
    }

    [void]Restore() {
        [string]$feed = $this.Version.Contains([char]'-') ? [BdnPackageSet]::NightlyFeed : [BdnPackageSet]::BaselineFeed
        $this.AssemblyFilePaths = [List[string]]::new()
        $this.AssemblyDirectoryPaths = [List[string]]::new()
        Start-NativeExecution { dotnet restore ([BdnPackageSet]::DownloadProjectFilePath) "-p:BdnFeed=$feed" "-p:BdnVersion=$($this.Version)" } -VerboseOutputOnError
        if (-not $this.IsRestored()) {
            throw "Restore of $($this.Name) $($this.Version) failed."
        }
    }
}

# .SYNOPSIS
#   Diff status. use Max for multiple results
enum DiffStatus {
    Unchanged
    NonBreaking
    Breaking
}

# .SYNOPSIS
#   Result produced by a single tool run
class DiffToolResult {
    [ValidateNotNullOrEmpty()][string]$ToolName
    [ValidateNotNullOrEmpty()][string]$NoResultsText = 'No differences.'
    [string]$Result
    [DiffStatus]$Status

    DiffToolResult([string]$toolName) {
        $this.ToolName = $toolName
        $this.Result = ''
    }

    DiffToolResult([string]$toolName, [string]$noResultsText) {
        $this.ToolName = $toolName
        $this.NoResultsText = $noResultsText
        $this.Result = ''
    }

    [string]GetStatusText() {
        switch ($this.Status) {
            ([DiffStatus]::Unchanged) { return $this.NoResultsText }
            ([DiffStatus]::NonBreaking) { return 'Non-breaking changes.' }
            ([DiffStatus]::Breaking) { return 'Breaking changes.' }
        }
        return $this.Status
    }
}

# .SYNOPSIS
#   Diff report with results from multiple tools
class DiffReport {
    [string]$WorkflowHeader
    [string]$Title = '## {0} {1} vs. {2}'
    [string]$Name
    [string]$ReferenceVersion
    [string]$DifferenceVersion
    [DiffToolResult[]]$ToolResults

    DiffReport([string]$name, [string]$referenceVersion, [string]$differenceVersion, [DiffToolResult[]]$toolResults) {
        $this.Name = $name
        $this.ReferenceVersion = $referenceVersion
        $this.DifferenceVersion = $differenceVersion
        $this.ToolResults = $toolResults
    }

    [DiffStatus]GetStatus() {
        if (-not $this.ToolResults) { return [DiffStatus]::Unchanged }
        return ($this.ToolResults | Measure-Object Status -Maximum).Maximum
    }

    [string]GetStatusText() {
        [DiffStatus]$status = $this.GetStatus()
        switch ($status) {
            ([DiffStatus]::Unchanged) { return 'No changes.' }
            ([DiffStatus]::NonBreaking) { return 'Non-breaking changes.' }
            ([DiffStatus]::Breaking) { return 'Breaking changes.' }
        }
        return $status
    }

    [bool]IsBreaking() {
        return $this.GetStatus() -eq [DiffStatus]::Breaking
    }

    [void]AddWorkflowHeader([string]$ownerRepo, [string]$workflowId, [long]$runId, [string]$badgeUri, [securestring]$token) {
        [string]$uri = "https://api.github.com/repos/$ownerRepo/actions/workflows/$workflowId"
        $workflow = Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $token
        $uri = "https://api.github.com/repos/$ownerRepo/actions/runs/$runId"
        $run = Invoke-RestMethod -Uri $uri -Authentication Bearer -Token $token
        # Note: $workflow.html_url points to the source file, not the overview of workflow runs.
        [string]$urlWorkflowRuns = "https://github.com/$ownerRepo/actions/workflows/$(Split-Path $workflow.path -Leaf)"
        $this.WorkflowHeader = "Workflow [$($workflow.name)]($urlWorkflowRuns) Run [#$($run.run_number)]($($run.html_url))"
        if ($badgeUri) {
            $this.WorkflowHeader += "`n`n[![]($badgeUri)]($($run.html_url))"
        }
    }

    hidden [DiffReport]AppendWorkflowHeader([StringBuilder]$sb) {
        if ($this.WorkflowHeader) { $sb.Append($this.WorkflowHeader).Append("`n`n") }
        return $this
    }

    hidden [DiffReport]AppendTitle([StringBuilder]$sb) {
        $sb.AppendFormat($this.Title, $this.Name, $this.ReferenceVersion, $this.DifferenceVersion).Append("`n`n")
        return $this
    }

    [string]AsIssue() {
        [StringBuilder]$sb = [StringBuilder]::new()
        $this.AppendWorkflowHeader($sb).AppendTitle($sb)
        foreach ($result in $this.ToolResults) {
            $sb.Append("<details$($result.Result ? '' : ' open')><summary><h3>").Append($result.ToolName).Append("</h3></summary>`n")
            if (-not $result.Result) {
                $sb.Append("`n").Append($result.NoResultsText).Append("`n`n")
            }
            else {
                [string]$s = [regex]::Replace($result.Result, '(?m-i)^#### ([^\n]*)$', "<details open><summary><b>`$1</b></summary>")
                $s = [regex]::Replace($s, '(?m-i)^```$', "```````n</details>")
                $sb.Append($s)
            }
            $sb.Append("</details>`n`n")
        }
        if ([Environment]::NewLine -cne "`n") { $sb.Replace("`n", [Environment]::NewLine) }
        return $sb.Length -le 65536 ? $sb.ToString() : $null
    }

    [string]AsSummaryIssue([bool]$reportTooBig) {
        [StringBuilder]$sb = [StringBuilder]::new()
        $this.AppendWorkflowHeader($sb).AppendTitle($sb)
        if ($reportTooBig) { $sb.Append('Results are too large to display.').Append("`n`n") }
        $sb.Append("$($this.GetStatusText()) See log artifacts of workflow run for details.").Append("`n`n")
        return $sb.ToString()
    }

    [string]AsLog() {
        [StringBuilder]$sb = [StringBuilder]::new()
        $this.AppendTitle($sb)
        foreach ($result in $this.ToolResults) {
            $sb.Append('### ').Append($result.ToolName).Append("`n`n")
            if (-not $result.Result) {
                $sb.Append($result.NoResultsText).Append("`n`n")
            }
            else {
                $sb.Append($result.Result)
            }
        }
        if ([Environment]::NewLine -cne "`n") { $sb.Replace("`n", [Environment]::NewLine) }
        return $sb.ToString()
    }
}

# .SYNOPSIS
#   Runs diff tools on given package sets and stores results
class DiffRunner {
    hidden static [string[]] $s_apiCompatExcludeAttributes = @(
        'T:System.Runtime.CompilerServices.IteratorStateMachineAttribute'
    )
    [ValidateNotNull()][BdnPackageSet]$ReferenceSet
    [ValidateNotNull()][BdnPackageSet]$DifferenceSet

    DiffRunner([BdnPackageSet]$referenceSet, [BdnPackageSet]$differenceSet) {
        $this.ReferenceSet = $referenceSet
        $this.DifferenceSet = $differenceSet
    }

    hidden [void]EnsureRestored() {
        if (-not $this.ReferenceSet.IsRestored()) { $this.ReferenceSet.Restore() }
        if (-not $this.DifferenceSet.IsRestored()) { $this.DifferenceSet.Restore() }
    }

    [DiffReport]RunAll([bool]$writeHost, [string]$prefix) {
        if (-not $writeHost) {
            function Write-Host {}
        }
        [BdnPackageSet[]]$needsRestore = @($this.ReferenceSet, $this.DifferenceSet).Where({ -not $_.IsRestored() })
        foreach ($set in $needsRestore) {
            $set.Restore()
            Write-Host "${prefix}Restored $($set.Name) $($set.Version)"
        }
        [DiffToolResult[]]$results = , $this.RunApiCompat()
        Write-Host "${prefix}$($results[0].ToolName): $($results[0].GetStatusText())"
        $results += $this.RunAsmDiff()
        Write-Host "${prefix}$($results[1].ToolName): $($results[1].GetStatusText())"
        $results += $this.RunAttrDiff()
        Write-Host "${prefix}$($results[2].ToolName): $($results[2].GetStatusText())"
        [string]$name = $this.ReferenceSet.Name -eq $this.DifferenceSet.Name ? $this.ReferenceSet.Name : $this.ReferenceSet.Name + ', ' + $this.DifferenceSet.Name
        return [DiffReport]::new($name, $this.ReferenceSet.Version, $this.DifferenceSet.Version, $results)
    }

    [DiffToolResult]RunApiCompat() {
        $this.EnsureRestored()
        [string]$excludeAttrFile = (New-TemporaryFile).FullName
        [string]$outFile = (New-TemporaryFile).FullName
        try {
            [DiffRunner]::s_apiCompatExcludeAttributes | Set-Content $excludeAttrFile
            [string[]]$params = @(
                $this.ReferenceSet.AssemblyFilePaths -join ','
                '--impl-dirs', ($this.DifferenceSet.AssemblyDirectoryPaths -join ',')
                '--left-operand', 'contract'
                '--right-operand', 'implementation'
                '--exclude-attributes', $excludeAttrFile
                '--out', $outFile
            )
            Invoke-ApiCompat $params -VerboseOutputOnError
            [DiffToolResult]$result = [DiffToolResult]::new('ApiCompat', 'No issues.')
            $messages = Get-Content $outFile | Split-ApiCompatMessage
            $issues = ($messages | Where-Object Issue -NE $null)?.Issue
            $errors = ($messages | Where-Object { $null -ne $_.Trace -and -not $_.Trace.IsResolver })?.Trace
            [array]$unknowns = ($messages | Where-Object Unknown -NE $null)?.Unknown
            [int]$total = ($messages | Measure-Object -Property Total -Sum)?.Sum
            [StringBuilder]$sb = [StringBuilder]::new()
            if ($errors -or $unknowns -or ${issues}?.Count + 0 -ne $total) {
                $result.Status = [DiffStatus]::Breaking
                $sb.AppendLine().AppendLine('```')
                foreach ($error in $errors) {
                    $sb.Append($error.Kind).Append(' ')
                    if ($error.Code -ne '0') { $sb.Append($error.Code).Append(' ') }
                    $sb.Append(': ').AppendLine($error.Message)
                }
                foreach ($unknown in $unknowns) {
                    $sb.AppendLine($unknown)
                }
                if (${issues}?.Count + 0 -ne $total) {
                    $sb.AppendLine("Error : Issue count of $(${issues}?.Count + 0) does not match reported count of $total.")
                }
                $sb.AppendLine('```')
            }
            if ($issues) {
                $result.Status = [DiffStatus]::Breaking
                $issues = $issues | Sort-Object { Format-ApiCompatSymbol $_.Parts[$_.TargetSymbolIndex] } -Stable -CaseSensitive
                $sb.AppendLine().AppendLine('```cs')
                # MembersMustExist and TypeMustExist are the most frequent ones
                [int]$padding = 'MembersMustExist'.Length
                foreach ($issue in $issues) {
                    $parts = $issue.Parts
                    for ([int]$i = 1; $i -lt $parts.Count; $i += 2) {
                        $parts[$i] = Format-ApiCompatSymbol $parts[$i]
                    }
                    $sb.AppendLine($issue.Rule.PadRight($padding) + ' : ' + $parts[$issue.TargetSymbolIndex])
                    if (-not $issue.IsSimple) {
                        $sb.Append('    // ').Append($parts[0])
                        for ([int]$i = 1; $i -lt $parts.Count; $i++) {
                            if ($i % 2 -eq 0) { $sb.AppendLine().Append('    //') }
                            $sb.Append($parts[$i])
                        }
                        if ($parts.Count % 2 -ne 0) { $sb.AppendLine() }
                    }
                }
                $sb.AppendLine('```')
            }
            if ($sb.Length -ne 0) { $sb.AppendLine() }
            $result.Result = $sb.ToString()
            if ([Environment]::NewLine -cne "`n") {
                $result.Result = $result.Result.ReplaceLineEndings("`n")
            }
            return $result
        }
        finally {
            Remove-Item $excludeAttrFile -ErrorAction Ignore
            Remove-Item $outFile -ErrorAction Ignore
        }
    }

    [DiffToolResult]RunAsmDiff() {
        $this.EnsureRestored()
        [string]$outFile = (New-TemporaryFile).FullName
        try {
            [string[]]$params = @(
                '--OldSet', ($this.ReferenceSet.AssemblyFilePaths -join ',')
                '--NewSet', ($this.DifferenceSet.AssemblyFilePaths -join ',')
                '--Removed', '--Added', '--Changed'
                '--DiffWriter', 'Markdown'
                '--OutFile', $outFile
            )
            Invoke-AsmDiff $params -VerboseOutputOnError
            [DiffToolResult]$result = [DiffToolResult]::new('AsmDiff')
            [string]$content = Get-Content $outFile -Raw
            if ([Environment]::NewLine -cne "`n") { $content = $content.ReplaceLineEndings("`n") }
            [int]$pos = $content.IndexOf("`n## ")
            if ($pos -ge 0) {
                $content = $content.Substring($pos)
                $result.Status = $content.IndexOf("`n-") -gt 0 ? [DiffStatus]::Breaking : [DiffStatus]::NonBreaking
                $result.Result = $content.Replace("`n## ", "`n#### ")
            }
            return $result
        }
        finally {
            Remove-Item $outFile -ErrorAction Ignore
        }
    }

    [DiffToolResult]RunAttrDiff() {
        $this.EnsureRestored()
        [string[]]$attrNames = 'PublicAPIAttribute', 'ObsoleteAttribute'
        [hashtable]$attrDiffs = Compare-AttributeUsage -ReferenceAssemblies $this.ReferenceSet.AssemblyFilePaths -DifferenceAssemblies $this.DifferenceSet.AssemblyFilePaths -Attributes $attrNames -Scope Public
        [DiffToolResult]$result = [DiffToolResult]::new('AttrDiff')
        [StringBuilder]$sb = [StringBuilder]::new()
        foreach ($attr in $attrNames) {
            [string[]]$diff = $attrDiffs[$attr]
            if ($diff) {
                $result.Status = [DiffStatus]::NonBreaking
                $sb.Append("`n#### ").Append($attr).Append("`n`n``````diff`n")
                $sb.AppendJoin("`n", $diff).Append("`n```````n`n")
            }
        }
        $result.Result = $sb.ToString()
        return $result
    }
}

################################################################################
#
#   Main
#
################################################################################

# Common parameter -Debug
[bool]$DebugIsPresent = $PSBoundParameters['Debug'] -eq $true

# GitHub data about current workflow run and repository
if (-not $env:GITHUB_RUN_ID -or -not $env:GITHUB_RUN_NUMBER -or
    -not $env:GITHUB_REPOSITORY -or -not $env:GITHUB_OUTPUT) {
    throw 'GitHub environment variables are not defined.'
}

# Add type [NuGetVersion]
Add-NuGetType -AssemblyName 'NuGet.Versioning'
Invoke-Expression 'using namespace NuGet.Versioning'


# Prepare artifacts
[hashtable]$lastRunStatus = @{
    LastCheckedVersion = [BdnPackageSet]::BaselineVersion
}
[string]$artifactDirectory = Join-Path ([Path]::GetTempPath()) ([Path]::GetRandomFileName())
$null = New-Item $artifactDirectory -ItemType 'Directory' -Force
[string]$statusFile = Join-Path $artifactDirectory 'BdnApiCompatStatus.json'
[string[]]$logFiles = @()
Write-Output "StatusArtifactName=$StatusArtifactName" >>$env:GITHUB_OUTPUT
Write-Output "StatusArtifactPath=$statusFile" >>$env:GITHUB_OUTPUT

# Download artifact from previous workflow run
[int]$runNumber = $env:GITHUB_RUN_NUMBER
[long]$workflowId = Get-WorkflowId $env:GITHUB_REPOSITORY $env:GITHUB_RUN_ID -Token $GitHubToken
if ($runNumber -gt 1) {
    $artifacts = Find-ArtifactsFromPreviousRun $env:GITHUB_REPOSITORY $StatusArtifactName -WorkflowId $workflowId `
        -MaxRunNumber ($runNumber - 1) -Token $GitHubToken
    if ($artifacts) {
        Write-Host "Found artifact '$StatusArtifactName' from workflow run #$(
            $artifacts.workflow_run.run_number) on $($artifacts.workflow_run.created_at) UTC."
        Expand-Artifact $artifacts.artifacts[0].archive_download_url $artifactDirectory -Token $GitHubToken
        $lastRunStatus = Get-Content $statusFile -Raw | ConvertFrom-Json -AsHashtable
    }
}

[string]$previousVersion = $lastRunStatus.LastCheckedVersion
if ($PreviousVersionOverride -and $PreviousVersionOverride -ne $previousVersion) {
    Write-Host "::warning::Overriding previous version $previousVersion with $PreviousVersionOverride"
    $previousVersion = $PreviousVersionOverride
}

# Check for new version and validate
Write-Host 'Searching for latest package version...'
$response = Invoke-RestMethod -Uri ([uri]::new([uri]::new([BdnPackageSet]::NightlyFeed), "flatcontainer/$([BdnPackageSet]::Infos[0].Name)/index.json"))
[string]$latestVersion = $response.versions.ForEach({ [NuGetVersion]$_ }) | Sort-Object -Descending | Select-Object -First 1

if ([NuGetVersion]$latestVersion -lt [NuGetVersion]$previousVersion) {
    throw "Latest version $latestVersion is lower than previous version $previousVersion"
}

$lastRunStatus.LastCheckedVersion = $latestVersion
Write-Output "LastCheckedVersion=$latestVersion" >>$env:GITHUB_OUTPUT

if ($latestVersion -eq $previousVersion) {
    Write-Host "::notice::Latest BDN version is $latestVersion (unchanged)"
    $lastRunStatus | ConvertTo-Json | Set-Content $statusFile
}
else {
    [List[string]]$notice = [List[string]]::new()
    Write-Host "Comparing BDN $previousVersion with latest version $latestVersion"
    [BdnPackageSet]$latestSet = [BdnPackageSet]::new($latestVersion)
    [BdnPackageSet]$previousSet = [BdnPackageSet]::new($previousVersion)
    [DiffReport]$incrementalReport = [DiffRunner]::new($previousSet, $latestSet).RunAll($true, '  ')
    $notice.Add("BDN $previousVersion vs. ${latestVersion}: $($incrementalReport.GetStatusText())")
    [DiffReport]$baselineReport = $null
    if ($incrementalReport.IsBreaking() -or $FullCompare.IsPresent -and $previousVersion -ne [BdnPackageSet]::BaselineVersion) {
        Write-Host "Comparing BDN $([BdnPackageSet]::BaselineVersion) with latest version $latestVersion"
        [BdnPackageSet]$baselineSet = [BdnPackageSet]::new([BdnPackageSet]::BaselineVersion)
        $baselineReport = [DiffRunner]::new($baselineSet, $latestSet).RunAll($true, '  ')
        $notice.Add("BDN $([BdnPackageSet]::BaselineVersion) vs. ${latestVersion}: $($baselineReport.GetStatusText())")
    }

    if ($incrementalReport.GetStatus() -ne [DiffStatus]::Unchanged -or $DebugIsPresent) {
        [string]$logfile = Join-Path $artifactDirectory 'incremental.md'
        $incrementalReport.AsLog() | Set-Content $logfile
        $logFiles += $logfile
    }
    if ($baselineReport -and ($baselineReport.GetStatus() -ne [DiffStatus]::Unchanged -or $DebugIsPresent)) {
        [string]$logfile = Join-Path $artifactDirectory 'baseline.md'
        $baselineReport.AsLog() | Set-Content $logfile
        $logFiles += $logfile
    }

    # Make sure artifacts are available even if we die later
    if ($logFiles) {
        Write-Output "LogArtifactName=$LogArtifactName" >>$env:GITHUB_OUTPUT
        Write-Output 'LogArtifactPath<<::', $logFiles, '::' >>$env:GITHUB_OUTPUT
    }
    $lastRunStatus | ConvertTo-Json | Set-Content $statusFile
    if ($incrementalReport.IsBreaking() -or $DebugIsPresent) {
        Write-Output 'IsBreaking=true' >>$env:GITHUB_OUTPUT
    }

    if ($IssueType -ne 'None' -and ($incrementalReport.IsBreaking() -or ${baselineReport}?.IsBreaking() -or $DebugIsPresent)) {
        [string]$incrementalIssue = $null
        [string]$baselineIssue = $null
        $incrementalReport.AddWorkflowHeader($env:GITHUB_REPOSITORY, $workflowId, $env:GITHUB_RUN_ID, $BadgeUri, $GitHubToken)
        if ($IssueType -in ('Report', 'ReportAndSummary')) {
            $incrementalIssue = $incrementalReport.AsIssue()
            if (-not $incrementalIssue) { $incrementalIssue = $incrementalReport.AsSummaryIssue($true) }
        }
        else {
            $incrementalIssue = $incrementalReport.AsSummaryIssue($false)
        }
        if ($baselineReport) {
            [bool]$reportTooBig = $false
            if ($IssueType -eq 'Report') {
                $baselineIssue = $baselineReport.AsIssue()
                if (-not $baselineIssue) { $reportTooBig = $true }
            }
            if (-not $baselineIssue) {
                $baselineIssue = $baselineReport.AsSummaryIssue($reportTooBig)
                # Try to not create extra comment for baseline summary.
                # Max issue character count is 65536, but we keep a wiggle room
                if ($incrementalIssue.Length + $baselineIssue.Length -lt 65000) {
                    $incrementalIssue += $baselineIssue
                    $baselineIssue = $null
                }
            }
        }

        [hashtable]$auth = @{
            Authentication = 'Bearer'
            Token          = $GitHubToken
        }
        [hashtable]$params = @{
            title = 'BDN API Compatibility Alert'
            body  = $incrementalIssue
        }
        if ($IssueLabels) { $params.labels = $IssueLabels }
        [string]$uri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/issues"
        $issue = $params | ConvertTo-Json -EscapeHandling EscapeNonAscii | Invoke-RestMethod -Uri $uri -Method Post @auth
        Write-Output "IssueNumber=$($issue.number)" >>$env:GITHUB_OUTPUT
        $notice.Add("Created issue #$($issue.number): $($issue.html_url)")
        if ($baselineIssue) {
            $params = @{ body = $baselineIssue }
            $uri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/issues/$($issue.number)/comments"
            $null = $params | ConvertTo-Json -EscapeHandling EscapeNonAscii | Invoke-RestMethod -Uri $uri -Method Post @auth
        }
    }
    # Note: While %0A will appear as newline in the console log,
    # the annotation in the workflw run summary will just be a single line.
    Write-Host "::notice::$($notice -join '%0A')"
}
