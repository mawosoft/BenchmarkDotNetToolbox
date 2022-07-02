# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Add types referenced by Pwsh classes declared in the root module.
#>

#Requires -Version 7

# This affects the caller scope, so we can't just use our preferences
# Set-StrictMode -Version 3.0
# $ErrorActionPreference = 'Stop'

$null = Get-Command -Name 'git' -ErrorAction Stop
. "$PSScriptRoot/../addNuGetType.ps1"
Add-NuGetType -ErrorAction Stop
