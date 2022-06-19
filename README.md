# Mawosoft Extensions for BenchmarkDotNet

[![NuGet](https://img.shields.io/nuget/v/Mawosoft.Extensions.BenchmarkDotNet.svg)](https://www.nuget.org/packages/Mawosoft.Extensions.BenchmarkDotNet/)
[![CI/CD](https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/actions/workflows/ci.yml/badge.svg)](https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/actions/workflows/ci.yml)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
![](https://img.shields.io/badge/netstandard-2.0-green.svg)

An extensions library to support benchmarking with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet).

### Overview · [Documentation](https://mawosoft.github.io/Mawosoft.Extensions.BenchmarkDotNet/) · [Samples](https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/tree/master/samples)

#### Column Display

- **`CombinedParamsColumn`**  
An alternative to `DefaultColumnProviders.Params` that displays all parameters in a single, customizable column.

- **`RecyclableParamsColumnProvider`**  
An alternative to `DefaultColumnProviders.Params` that displays parameters in recyclable columns corresponding to parameter position rather than name.

- **`ParamWrapper<T>`**  
A generic wrapper to associate strongly typed parameter or argument values with a display text.

- **`JobColumnSelectionProvider`**  
An alternative to `DefaultColumnProviders.Job`, with a user-defined selection of Job columns.

#### Configuration and Running

- **`BenchmarkRunInfos`**  
A wrapper and extension for `BenchmarkConverter`, collecting the converted benchmarks, executing them, and optionally overriding any global and local Job configurations.

- **`ConfigExtensions`**  
`ManualConfig` and `IConfig` extension methods for replacing parts of an existing config - for example, default columns with custom ones.

- **`WhatifFilter`**  
An alternative to BenchmarkDotNet's `--list` command line option that prints a mock summary of all available benchmarks according to the current effective BechmarkDotnet configuration.

### [CI Feed](https://dev.azure.com/mawosoft-de/public/_packaging?_a=feed&feed=public)

To consume the latest CI build, add the following feed to your `Nuget.Config`:
```
<configuration>
  <packageSources>
    <add key="mawosoft-nightly" value="https://pkgs.dev.azure.com/mawosoft-de/public/_packaging/public/nuget/v3/index.json" />
  </packageSources>
</configuration>
```
The CI builds have the next expected version number and are tagged as `-dev.<number>`.
- Example of an official release: `Mawosoft.Extensions.BenchmarkDotNet.0.2.2.nupkg`
- Example of CI build: `Mawosoft.Extensions.BenchmarkDotNet.0.2.3-dev.98.nupkg`  
To always use the latest CI build, use a package reference like  
`<PackageReference Include="Mawosoft.Extensions.BenchmarkDotNet" Version="*-*" />`

