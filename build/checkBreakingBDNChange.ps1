# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Check latest nightly version of BenchmarkDotNet packages for breaking API changes.

.DESCRIPTION
    Check latest nightly version of BenchmarkDotNet packages for breaking API changes.
    Discovers new versions to check via artifacts from previous workflow runs. The script will
    download the previous artifact, but the caller is responsible for uploading the new one.
    Creates an issue, if new breaking changes have been found.

.OUTPUTS
    None. Sets workflow step outputs 'ArtifactName' and 'ArtifactPath' via workflow commands
    issued by Write-Host.
#>

using namespace System.IO
using namespace System.Collections.Generic
using namespace Microsoft.PackageManagement.Packaging

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [Alias('Token', 't')]
    [securestring]$GitHubToken,
    [ValidateNotNullOrEmpty()]
    [Alias('Artifact', 'a')]
    [string]$ArtifactName = 'BreakingBDNChange',
    [Alias('Labels', 'l')]
    [string[]]$IssueLabels = @('BDN change')
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

Import-Module "$PSScriptRoot/ApiDiffHelper.psm1" -Force
Import-Module "$PSScriptRoot/GitHubHelper.psm1" -Force
Add-NuGetVersioning

class PackageDiff {
    [string]$Name
    [string]$BaselineVersion
    [string]$PreviousVersion
    [string]$CurrentVersion
    [PSCustomObject]$BaselineDiff
    [PSCustomObject]$IncrementalDiff
    PackageDiff([string]$name, [string]$baselineVersion) {
        $this.Name = $name
        $this.BaselineVersion = $baselineVersion
    }
}

# Packages to process with their baseline versions
[List[PackageDiff]]$packageDiffs = [List[PackageDiff]]::new()
$packageDiffs.Add([PackageDiff]::new('BenchmarkDotNet', '0.13.1'))
$packageDiffs.Add([PackageDiff]::new('BenchmarkDotNet.Annotations', '0.13.1'))
$packageDiffs.Add([PackageDiff]::new('BenchmarkDotNet.Diagnostics.Windows', '0.13.1'))

# NuGet feeds
[string]$baselineFeed = 'https://api.nuget.org/v3/index.json'
[string]$nightlyFeed = 'https://ci.appveyor.com/nuget/benchmarkdotnet'

# GitHub data about current workflow run and repository
[long]$runId = $env:GITHUB_RUN_ID
[int]$runNumber = $env:GITHUB_RUN_NUMBER
[string]$ownerRepo = $env:GITHUB_REPOSITORY
if (-not $runId -or -not $runNumber -or -not $ownerRepo) {
    throw "GitHub environment variables are not defined."
}


# Download artifact from previous workflow run
[string]$artifactDirectory = Join-Path ([Path]::GetTempPath()) ([Path]::GetRandomFileName())
[string]$artifactFile = Join-Path $artifactDirectory 'LastChecked.json'
$null = [Directory]::CreateDirectory($artifactDirectory)
if ($runNumber -gt 1) {
    [long]$workflowId = Get-WorkflowId $ownerRepo $runId -Token $GitHubToken
    $artifacts = Find-ArtifactsFromPreviousRun $ownerRepo $ArtifactName -WorkflowId $workflowId `
        -MaxRunNumber ($runNumber - 1) -Token $GitHubToken
    if ($artifacts) {
        Write-Host "Found artifact '$ArtifactName' from workflow run #$(
            $artifacts.workflow_run.run_number) on $($artifacts.workflow_run.created_at) UTC."
        Expand-Artifact $artifacts.artifacts[0].archive_download_url $artifactDirectory -Token $GitHubToken
        [hashtable]$lastCompared = Get-Content $artifactFile -Raw | ConvertFrom-Json -AsHashtable
        $packageDiffs | ForEach-Object {
            $_.PreviousVersion = $lastCompared[$_.Name]
        }
    }
}

# Find latest versions and run new comparisons if necessary
Write-Host "Searching for latest package versions..."
$packageDiffs | ForEach-Object {
    [SoftwareIdentity]$currentPkg = Find-Package $_.Name -Source $nightlyFeed -AllowPrereleaseVersions
    $_.CurrentVersion = $currentPkg.Version
    Write-Host "$($_.Name) $($_.CurrentVersion)" -NoNewline
    if ($_.CurrentVersion -eq $_.PreviousVersion) {
        Write-Host " (version unchanged)"
    }
    else {
        if (-not $_.PreviousVersion) {
            Write-Host " (no previous version)"
        }
        else {
            Write-Host " (was $($_.PreviousVersion))"
            if ([NuGet.Versioning.NuGetVersion]$_.CurrentVersion -lt
                [NuGet.Versioning.NuGetVersion]$_.PreviousVersion) {
                throw "Find-Package failed to find latest version."
            }
            [SoftwareIdentity]$previousPkg = Find-Package $_.Name -RequiredVersion $_.PreviousVersion `
                -Source $nightlyFeed
            $_.IncrementalDiff = Invoke-NuGetDiff $previousPkg $currentPkg | ConvertFrom-NuGetDiff
            if ($_.IncrementalDiff.AssemblyDiffs.Count -eq 0) {
                $_.IncrementalDiff = $null
            }
            Write-Host "  Found$(if (-not $_.IncrementalDiff) { ' no' }) breaking changes comparing $(
                $_.PreviousVersion) <-> $($_.CurrentVersion)"
        }
        if ($_.IncrementalDiff -or -not $_.PreviousVersion) {
            [SoftwareIdentity]$baselinePkg = Find-Package $_.Name -RequiredVersion $_.BaselineVersion `
                -Source $baselineFeed
            $_.BaselineDiff = Invoke-NuGetDiff $baselinePkg $currentPkg | ConvertFrom-NuGetDiff
            if ($_.BaselineDiff.AssemblyDiffs.Count -eq 0) {
                $_.BaselineDiff = $null
            }
            Write-Host "  Found$(if (-not $_.BaselineDiff) { ' no' }) breaking changes comparing $(
                $_.BaselineVersion) <-> $($_.CurrentVersion)"
        }
    }
}

# Create an issue if new breaking changes have been discovered
$breaking = $packageDiffs | Where-Object { $_.IncrementalDiff -or $_.BaselineDiff }
if ($breaking) {
    $version = $breaking | Select-Object -Property CurrentVersion -Unique
    if ($version.Count -eq 1) {
        [string]$title = "$($breaking.Name -join ', ') ``$($version.CurrentVersion)``"
    }
    else {
        [string]$title = ($breaking | ForEach-Object { "$($_.Name) ``$($_.CurrentVersion)``" }) -join ', '
    }
    $title = "New breaking change: $title"
    [System.Text.StringBuilder]$body = [System.Text.StringBuilder]::new()
    $breaking | ForEach-Object {
        $_.IncrementalDiff, $_.BaselineDiff | ForEach-Object {
            if ($_) {
                $null = $body.AppendLine(
                    "## $($_.Reference.Name) $($_.Reference.Version) <-> $($_.Difference.Version)")
                $_.AssemblyDiffs | ForEach-Object {
                    $null = $body.AppendLine("**$($_.Name) [$($_.Frameworks -join ', ')]**")
                    $null = $body.AppendLine('<details><summary>Breaking</summary>')
                    $null = $body.AppendLine()
                    $null = $body.Append($_.Breaking)
                    $null = $body.AppendLine('</details>')
                    $null = $body.AppendLine('<details><summary>Diff</summary>')
                    $null = $body.AppendLine()
                    $null = $body.Append($_.Diff)
                    $null = $body.AppendLine('</details>')
                    $null = $body.AppendLine()
                }
            }
        }
    }
    [hashtable]$params = @{
        title = $title
        body  = $body.ToString()
    }
    if ($IssueLabels) { $params.labels = $IssueLabels }
    $issue = $params | ConvertTo-Json | Invoke-RestMethod -Uri "https://api.github.com/repos/$ownerRepo/issues" `
        -Method Post -Authentication Bearer -Token $token
    Write-Host "Created issue #$($issue.number)"
}

[hashtable]$lastCompared = @{}
$packageDiffs | ForEach-Object { $lastCompared.Add($_.Name, $_.CurrentVersion) }
$lastCompared | ConvertTo-Json | Set-Content $artifactFile

Write-Host "::set-output name=ArtifactName::$ArtifactName"
Write-Host "::set-output name=ArtifactPath::$artifactFile"
