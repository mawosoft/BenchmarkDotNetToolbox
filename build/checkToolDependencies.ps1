# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Check tool dependencies.

.DESCRIPTION
    Checks dependencies on tools produced by monorepos by tracking changes to relevant portions
    of the monorepo and reporting them. Discovers changes by comparing the results to artifacts
    from previous workflow runs The script will download the previous artifact, but the caller
    is responsible for uploading the new one.
    Creates an issue, if new dependency problems have been found.

.OUTPUTS
    None. Sets the following workflow step output parameters via workflow commands issued by Write-Host.
        ArtifactName - Artifact name for the result file to share between workflow runs.
        ArtifactPath - Artifact path for the result file.
        IssueNumber  - Issue with report of tool dependency problems.
#>

#Requires -Version 7.2

using namespace System
using namespace System.Collections.Generic
using namespace System.IO
using namespace System.Management.Automation
using namespace System.Text

[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [Alias('Token')]
    [securestring]$GitHubToken,

    [ValidateNotNullOrEmpty()]
    [string]$ArtifactName = 'ToolDependencyCheck',

    [Alias('Labels')]
    [string[]]$IssueLabels = @('dependencies')
)

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

# GitHub data about current workflow run and repository
if (-not $env:GITHUB_RUN_ID -or -not $env:GITHUB_RUN_NUMBER -or
    -not $env:GITHUB_REPOSITORY -or -not $env:GITHUB_OUTPUT) {
    throw 'GitHub environment variables are not defined.'
}

Import-Module "$PSScriptRoot/GitHubHelper.psm1" -Force
Import-Module "$PSScriptRoot/MonorepoDependencies/MonorepoDependencies.psd1" -Force
Invoke-Expression "using module $PSScriptRoot/MonorepoDependencies/MonorepoDependencies.psd1"

# Monorepo tools definition
[MonorepoDependencies]$monorepos = [MonorepoDependencies]::new(@(
        [MonorepoPackage]::new(
            'Microsoft.DotNet.ApiCompat',
            'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json',
            "$PSScriptRoot/ApiCompat/ToolRestore/ToolRestore.proj",
            'https://github.com/dotnet/arcade',
            @(
                'src/Microsoft.DotNet.ApiCompat',
                'src/Microsoft.Cci.Extensions'
            )
        ),
        [MonorepoPackage]::new(
            'Microsoft.DotNet.AsmDiff',
            'https://pkgs.dev.azure.com/dnceng/public/_packaging/dotnet-eng/nuget/v3/index.json',
            "$PSScriptRoot/ApiCompat/ToolRestore/ToolRestore.proj",
            'https://github.com/dotnet/arcade',
            @(
                'src/Microsoft.DotNet.AsmDiff',
                'src/Microsoft.Cci.Extensions'
            )
        )
    ))
$monorepos.GitHubToken = $GitHubToken
# Get tool versions in use
$monorepos.UpdateFromManifestOrProjectFile()

# Prepare artifacts
[string]$artifactDirectory = Join-Path ([Path]::GetTempPath()) ([Path]::GetRandomFileName())
$null = New-Item $artifactDirectory -ItemType 'Directory' -Force
[string]$monoreposFile = Join-Path $artifactDirectory 'MonorepoDependencies.json'

# Download artifact from previous workflow run
[int]$runNumber = $env:GITHUB_RUN_NUMBER
[long]$workflowId = Get-WorkflowId $env:GITHUB_REPOSITORY $env:GITHUB_RUN_ID -Token $GitHubToken
if ($runNumber -gt 1) {
    $artifacts = Find-ArtifactsFromPreviousRun $env:GITHUB_REPOSITORY $ArtifactName -WorkflowId $workflowId `
        -MaxRunNumber ($runNumber - 1) -Token $GitHubToken
    if ($artifacts) {
        Write-Host "Found artifact '$ArtifactName' from workflow run #$(
            $artifacts.workflow_run.run_number) on $($artifacts.workflow_run.created_at) UTC."
        Expand-Artifact $artifacts.artifacts[0].archive_download_url $artifactDirectory -Token $GitHubToken
        $monorepos.UpdateFromJson((Get-Content $monoreposFile -Raw))
    }
}

# Update and save changes to artifact
$monorepos.UpdateLatest()
$monorepos.ToJson() | Set-Content $monoreposFile
Write-Output "ArtifactName=$ArtifactName" >>$env:GITHUB_OUTPUT
Write-Output "ArtifactPath=$monoreposFile" >>$env:GITHUB_OUTPUT

# Report updated paths and outdated packages
# GetNewOutdatedPackages() accounts for the following special case:
# - During a previous run, updated paths were detected, but the latest package version was not yet updated.
# - During the current run, no paths were updated, but an up-to-date package version is now available.
[KeyValuePair[string, MonorepoPackagePath[]][]]$updatedRepoPaths = $monorepos.GetUpdatedPaths()
if (-not $updatedRepoPaths -and -not $monorepos.GetNewOutdatedPackages()) {
    Write-Host 'Found no monorepo updates.'
}
else {
    [MonorepoPackage[]]$outdatedPackages = $monorepos.GetAllOutdatedPackages()

    [StringBuilder]$console = [StringBuilder]::new()
    [StringBuilder]$markdown = [StringBuilder]::new()
    [string]$accent = ''
    [string]$reset = ''
    if ($global:PSStyle.OutputRendering -eq [OutputRendering]::Ansi) {
        $accent = $global:PSStyle.Formatting.FormatAccent
        $reset = $global:PSStyle.Reset
    }

    [hashtable]$auth = @{
        Authentication = 'Bearer'
        Token          = $GitHubToken
    }

    [string]$uri = "https://api.github.com/repos/$($env:GITHUB_REPOSITORY)/actions/workflows/$workflowId"
    $workflow = Invoke-RestMethod -Uri $uri @auth
    $uri = "https://api.github.com/repos/$($env:GITHUB_REPOSITORY)/actions/runs/$($env:GITHUB_RUN_ID)"
    $run = Invoke-RestMethod -Uri $uri @auth
    # Note: $workflow.html_url points to the source file, not the overview of workflow runs.
    [string]$urlWorkflowRuns = "https://github.com/$($env:GITHUB_REPOSITORY)/actions/workflows/$(Split-Path $workflow.path -Leaf)"
    $null = $markdown.AppendLine("Workflow [$($workflow.name)]($urlWorkflowRuns) Run [#$($run.run_number)]($($run.html_url))")

    if ($updatedRepoPaths) {
        $null = $markdown.AppendLine().AppendLine('### Updated Paths')
        $null = $console.AppendLine().Append($accent).Append('Updated Paths').AppendLine($reset)
        foreach ($kvp in $updatedRepoPaths) {
            $null = $markdown.AppendLine().Append('#### ').AppendLine($kvp.Key)
            $null = $console.AppendLine().Append($accent).Append($kvp.Key).AppendLine($reset)
            foreach ($path in $kvp.Value) {
                $null = $markdown.AppendLine().Append('- ').AppendLine($path.Path).AppendLine('```')
                $null = $markdown.AppendJoin([Environment]::NewLine, $path.LatestAheadLog).AppendLine().AppendLine('```')
                $null = $console.AppendLine().Append($accent).Append('- ').Append($path.Path).AppendLine($reset)
                $null = $console.AppendJoin([Environment]::NewLine, $path.LatestAheadLog).AppendLine()
            }
        }

    }

    if ($outdatedPackages) {
        $null = $markdown.AppendLine().AppendLine('### Outdated Packages')
        $null = $console.AppendLine().Append($accent).Append('Outdated Packages').AppendLine($reset)
        foreach ($package in $outdatedPackages) {
            $null = $markdown.AppendLine().Append('- ').AppendLine($package.Name).AppendLine('```')
            $null = $console.AppendLine().Append($accent).Append('- ').Append($package.Name).AppendLine($reset)
            [string]$line = "Version: $($package.Version?.Version)  ($($package.Version?.AheadOfPathsCount))"
            $null = $markdown.AppendLine($line)
            $null = $console.AppendLine($line)
            [string]$line = "Latest:  $($package.LatestVersion?.Version)  ($($package.LatestVersion?.AheadOfPathsCount))"
            $null = $markdown.AppendLine($line)
            $null = $console.AppendLine($line)

            $null = $markdown.AppendJoin([Environment]::NewLine, $package.VersionBehindLog).AppendLine().AppendLine('```')
            $null = $console.AppendJoin([Environment]::NewLine, $package.VersionBehindLog).AppendLine()
        }
    }

    Write-Host $console.ToString()

    [hashtable]$params = @{
        title = 'Tool Dependency Alert'
        body  = $markdown.ToString()
    }
    if ($IssueLabels) { $params.labels = $IssueLabels }
    $uri = "https://api.github.com/repos/$env:GITHUB_REPOSITORY/issues"
    $issue = $params | ConvertTo-Json -EscapeHandling EscapeNonAscii | Invoke-RestMethod -Uri $uri -Method Post @auth
    Write-Output "IssueNumber=$($issue.number)" >>$env:GITHUB_OUTPUT
    Write-Host "::notice::Created Tool Dependency Alert. Issue #$($issue.number): $($issue.html_url)"
}
