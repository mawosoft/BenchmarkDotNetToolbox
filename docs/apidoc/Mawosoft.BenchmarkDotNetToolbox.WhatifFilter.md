---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter
seealso:
- linkType: HRef
  linkId: https://github.com/mawosoft/BenchmarkDotNetToolbox/tree/master/samples
  altText: What-If Filter Sample on GitHub
---
A *configuration* is the interplay of console arguments, global and local configs, and attributes.
The `WhatifFilter` can be controlled either programmatically via the 
@"Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.Enabled" property, or by using
the `--whatif` option (short: `-w`) on the command line and passing the console arguments through
@"Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.PreparseConsoleArguments(System.String[])?text=PreparseConsoleArguments()".

If enabled, the filter will collect all benchmark cases created by
@"BenchmarkDotNet.Running.BenchmarkRunner",
@"BenchmarkDotNet.Running.BenchmarkSwitcher",
or @"BenchmarkDotNet.Running.BenchmarkConverter"
and suppress their execution.

The properties @"Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.FilteredBenchmarkCases"
and @"Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.FilteredBenchmarkRunInfos"
provide access to the collected benchmarks,
while @"Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.PrintAsSummaries(BenchmarkDotNet.Loggers.ILogger)?text=PrintAsSummaries()"
can output them to the console or another logger.

---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter
example:
- *content
---
```csharp
public static void Main(string[] args)
{
    // Create the WhatifFilter and let it process the --whatif argument if one exists.
    WhatifFilter whatifFilter = new();
    args = whatifFilter.PreparseConsoleArguments(args);

    // Create a global confing and add the filter (DefaultConfig used for simplicity).
    ManualConfig config = DefaultConfig.Instance.AddFilter(whatifFilter);

    // Run the benchmarks as you would normally do. If the filter was enabled,
    // execution has been supressed and the returned array will be empty.
    Summary[] summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);

    // Check the filter and print the What-if summaries
    if (whatifFilter.Enabled)
    {
        whatifFilter.PrintAsSummaries(ConsoleLogger.Default);
        whatifFilter.Clear(dispose: true);
    }
}
```

---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.Enabled
summary: Gets or sets the filter's *Enabled* state.
syntax:
    return:
      description: |
        If <code>true</code>, the filter will collect all benchmark cases created by
        @"BenchmarkDotNet.Running.BenchmarkRunner" or related classes, and suppress their execution.
        If <code>false</code>, the filter is disabled and will not have any effect.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.FilteredBenchmarkCases
syntax:
    return:
      description: The collection of the filtered, individual benchmark cases.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.FilteredBenchmarkRunInfos
syntax:
    return:
      description: The collection of the filtered, grouped benchmark cases.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.PreparseConsoleArguments(System.String[])
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.Clear(System.Boolean)
syntax:
    parameters:
    - id: dispose
      description: "`true` to dispose all benchmarks, `false` to only remove them from the list without disposal."
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.PrintAsSummaries(BenchmarkDotNet.Loggers.ILogger)
syntax:
    parameters:
    - id: logger
      description: The logger to use for printing the summaries.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.WhatifFilter.Predicate(BenchmarkDotNet.Running.BenchmarkCase)
---
