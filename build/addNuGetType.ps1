# Copyright (c) 2022 Matthias Wolf, Mawosoft.

<#
.SYNOPSIS
    Adds NuGet assemblies via Add-Type
#>

#Requires -Version 7

using namespace System
using namespace System.Reflection

# .SYNOPSIS
#   Adds NuGet assemblies via Add-Type
function Add-NuGetType {
    [CmdletBinding()]
    param(
        # The NuGet assembly names to add types from. The default is NuGet.Protocol and its dependencies
        [ValidateNotNullOrEmpty()]
        [ValidatePattern('^NuGet\.')]
        [Alias('AN')]
        [string[]]$AssemblyName = @('NuGet.Protocol', 'NuGet.Packaging', 'NuGet.Versioning', 'NuGet.Configuration', 'NuGet.Common', 'NuGet.Frameworks')
    )

    [string]$nugetBinDirectoryPath = $null
    [Assembly[]]$nugetasm = [AppDomain]::CurrentDomain.GetAssemblies().Where({ $_.FullName.StartsWith('NuGet.', [StringComparison]::OrdinalIgnoreCase) })
    if ($nugetasm) {
        $nugetBinDirectoryPath = Split-Path $nugetasm[0].Location -Parent
    }
    else {
        [string]$projContent = @'
<Project DefaultTargets="GetNuGetBinDirectory">

  <PropertyGroup>
    <ImportDirectoryBuildProps>false</ImportDirectoryBuildProps>
    <ImportDirectoryPackagesProps>false</ImportDirectoryPackagesProps>
    <ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
    <ManagePackageVersionsCentrally>false</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <Import Project="Sdk.props" Sdk="Microsoft.NET.Sdk" />

  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <DisableImplicitFrameworkReferences>true</DisableImplicitFrameworkReferences>
    <NoBuild>true</NoBuild>
  </PropertyGroup>

  <Target Name="GetNuGetBinDirectory">
    <Message Importance="High" Text="NuGetBinDirectoryPath=$([System.IO.Path]::GetDirectoryName('$(NuGetPropsFile)'))" />
  </Target>

  <Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk" />

</Project>
'@
        [string]$projFilePath = (New-TemporaryFile).FullName
        $projContent | Set-Content $projFilePath
        [string[]]$output = dotnet msbuild $projFilePath -v:m --nologo
        Remove-Item $projFilePath -ErrorAction Ignore
        if (($output -join "`n") -notmatch '(?m-i)(?<=\bNuGetBinDirectoryPath=).*$') {
            throw "Unable to find NuGet assembly location"
            return
        }
        $nugetBinDirectoryPath = $Matches[0]
    }
    [string[]]$nugetasmNames = $nugetasm ? $nugetasm.GetName().Name : $null
    [string[]]$paths = $AssemblyName.Where({ $_ -notin $nugetasmNames }).ForEach({ Join-Path $nugetBinDirectoryPath ($_ + '.dll') })
    if ($paths) {
        Add-Type -Path $paths
    }
}
