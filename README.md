# Mawosoft Extensions for BenchmarkDotNet

[![NuGet](https://img.shields.io/nuget/v/Mawosoft.Extensions.BenchmarkDotNet.svg)](https://www.nuget.org/packages/Mawosoft.Extensions.BenchmarkDotNet/)
![](https://img.shields.io/badge/netstandard-2.0-green.svg)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

An extensions library to support benchmarking with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet).

### Overview · [Documentation](https://mawosoft.github.io/Mawosoft.Extensions.BenchmarkDotNet/) · [Samples](https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/tree/master/samples)

<table>
  <thead><tr>
    <th colspan=2>Column Display</th>
  </tr></thead>
  <tbody><tr valign=top>
    <td><code>CombinedParamsColumn</code></td>
    <td>An alternative to <code>DefaultColumnProviders.Params</code> that displays all parameters in a single, customizable column.</td>
  </tr>
  <tr valign=top>
    <td><code>RecyclableParamsColumnProvider</code></td>
    <td>An alternative to <code>DefaultColumnProviders.Params</code> that displays parameters in recyclable columns corresponding to parameter position rather than name.</td>
  </tr>
  <tr valign=top>
    <td><code>ParamWrapper&lt;T&gt;</code></td>
    <td>A generic wrapper to associate strongly typed parameter or argument values with a display text.</td>
  </tr>
  <tr valign=top>
    <td><code>JobColumnSelectionProvider</code></td>
    <td>An alternative to <code>DefaultColumnProviders.Job</code>, with a user-defined selection of Job columns.</td>
  </tr></tbody>
  <thead><tr>
    <th colspan=2>Configuration and Running</th>
  </tr></thead>
  <tbody><tr valign=top>
    <td><code>BenchmarkRunInfos</code></td>
    <td>A wrapper and extension for <code>BenchmarkConverter</code>, collecting the converted benchmarks, executing them, and optionally overriding any global and local Job configurations.</td>
  </tr>
  <tr valign=top>
    <td><code>ConfigExtensions</code></td>
    <td><code>ManualConfig</code> and <code>IConfig</code> extension methods for replacing parts of an existing config - for example, default columns with custom ones.</td>
  </tr>
  <tr valign=top>
    <td><code>WhatifFilter</code></td>
    <td>An alternative to BenchmarkDotNet's <code>--list</code> command line option that prints a mock summary of all available benchmarks according to the current effective BechmarkDotnet configuration.</td>  </tr></tbody>
</table>
