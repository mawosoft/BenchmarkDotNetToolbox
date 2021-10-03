# BenchmarkDotNet Toolbox

An assortment of classes to support benchmarking with [BenchmarkDotNet](https://github.com/dotnet/BenchmarkDotNet).

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
    <td>An alternative to <code>DefaultColumnProviders.Job</code> with user-defined selection of Job columns.</td>
  </tr></tbody>
  <thead><tr>
    <th colspan=2>Configuration and Running</th>
  </tr></thead>
  <tbody><tr valign=top>
    <td><code>BenchmarkRunInfos</code></td>
    <td>A wrapper and extension for <code>BenchmarkConverter</code>, collecting the converted benchmarks, executing them, and optionally overriding any global and local Job configurations.</td>
  </tr>
  <tr valign=top>
    <td><code>ManualConfigExtensions</code></td>
    <td>Extension methods for <code>ManualConfig</code> and <code>IConfig</code> in general. Amongst other things, they allow you to easily replace default columns with custom ones.</td>
  </tr>
  <tr valign=top>
    <td><code>WhatifFilter</code></td>
    <td>A filter that allows you to see results of a particular BechmarkDotnet configuration without actually running the benchmarks.</td>
  </tr></tbody>
</table>

For a deeper look, see the [documentation](https://mawosoft.github.io/BenchmarkDotNetToolbox/) and [samples](https://github.com/mawosoft/BenchmarkDotNetToolbox/tree/master/samples) (still work-in-progress).
