# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Finds the latest version of the BenchmarkDotNet CI (nightly) build
    that was build on AppVeyor directly from the master branch.
.OUTPUTS
    The package version as string.
.NOTES
    The BDN build system currently publishes any successful build to the AppVeyor NuGet feed.
    This includes PRs and builds from non-master branches. During their development, both may
    diverge severly from the history of the master branch, disrupting a CONTINUOUS view of
    accepted changes via NuGet packages.
#>

#Requires -Version 7

using namespace System

[CmdletBinding()]
[OutputType([string])]
param ()

Set-StrictMode -Version 3.0
$ErrorActionPreference = 'Stop'

[string]$historyUrl = 'https://ci.appveyor.com/api/projects/dotnetfoundation/benchmarkdotnet/history?recordsNumber=10'

Write-Verbose ('GET ' + $historyUrl)
$history = Invoke-RestMethod -Uri $historyUrl
[long]$skipBuildNumber = 0

for (; ; ) {
    foreach ($build in $history.builds) {
        [long]$buildId = $build.buildId
        [string]$pr = $build.psobject.Properties['pullRequestId']?.Value
        if ($build.status -eq 'success' -and $build.buildNumber -ne $skipBuildNumber) {
            if (-not $pr -and $build.branch -eq 'master') {
                Write-Verbose "Found: $($build.version)"
                return $build.version
            }
            $skipBuildNumber = $build.buildNumber
        }
        Write-Verbose "Skipped: $($build.version) $($build.status) $($build.branch) $(if ($pr) { "PR $pr" })"
    }
    [string]$url = "$historyUrl&startBuildId=$buildId"
    Write-Verbose ('GET ' + $url)
    $history = Invoke-RestMethod -Uri $url
}
