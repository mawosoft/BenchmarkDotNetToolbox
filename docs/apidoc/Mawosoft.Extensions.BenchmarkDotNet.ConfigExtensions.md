---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions
seealso:
- linkType: HRef
  linkId: https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/tree/master/samples
  altText: Config Extension Samples and Column Display Samples on GitHub
---
When building a config in *BenchmarkDotNet* to run your benchmarks, there are essentially two ways of doing it:
- You can start from scratch with an empty config and most likely end up forgetting to add some essential item.
- You can use the predefined
@"BenchmarkDotNet.Configs.DefaultConfig.Instance?displayProperty=nameWithType"
and probably get some ingredients you don't have any use for, but which are hard to get rid of
once they are part of the config.

This is where these extensions come in handy. You can use the default config - or any other preexisting one -
and replace parts of it. Don't like the default three exporters (HTML, CSV, GitHub Markdown) and prefer the leaner
Console Markdown exporter?
```csharp
ManualConfig config = DefaultConfig.Instance.ReplaceExporters(MarkdownExporter.Console);
```

Only want the statistical Mean column, not all the other ones, like StdDev and Error?
```csharp
ManualConfig config = DefaultConfig.Instance.ReplaceColumnCategory(StatisticColumn.Mean);
```

Which of course leads to the custom columns that are part of this library:
```csharp
ManualConfig config = DefaultConfig.Instance.ReplaceColumnCategory(
    new JobColumnSelectionProvider("-run +runstrategy"),
    new RecyclableParamsColumnProvider()
    );
```

> [!Note]
> When you use the extensions with a @"BenchmarkDotNet.Configs.ManualConfig", that config is modified and returned.
>
> When you use the extensions with any other @"BenchmarkDotNet.Configs.IConfig" (as in the examples above), the source config
> remains untouched and a new instance of @"BenchmarkDotNet.Configs.ManualConfig" with the changes applied is returned.


---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.IColumn[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newColumns
      description: The new columns to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Columns.IColumn[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newColumns
      description: The new columns to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.IColumnProvider[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newColumnProviders
      description: The new column providers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Columns.IColumnProvider[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newColumnProviders
      description: The new column providers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.RemoveColumnsByCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.ColumnCategory[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: categories
      description: The column categories to remove.
---
> [!NOTE]
> In *BenchmarkDotNet*, the @"BenchmarkDotNet.Columns.ColumnCategory.Job?displayProperty=nameWithType" encompasses not only the
> real job characteristic columns, but also the @"BenchmarkDotNet.Columns.TargetMethodColumn?text=method descriptor columns" and the
> @"BenchmarkDotNet.Columns.CategoriesColumn?text=categories column".
>
> This method **will not** remove the latter two column types if the removal of
> @"BenchmarkDotNet.Columns.ColumnCategory.Job?displayProperty=nameWithType" is requested.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.RemoveColumnsByCategory(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Columns.ColumnCategory[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: categories
      description: The column categories to remove.
---
> [!NOTE]
> In *BenchmarkDotNet*, the @"BenchmarkDotNet.Columns.ColumnCategory.Job?displayProperty=nameWithType" encompasses not only the
> real job characteristic columns, but also the @"BenchmarkDotNet.Columns.TargetMethodColumn?text=method descriptor columns" and the
> @"BenchmarkDotNet.Columns.CategoriesColumn?text=categories column".
>
> This method **will not** remove the latter two column types if the removal of
> @"BenchmarkDotNet.Columns.ColumnCategory.Job?displayProperty=nameWithType" is requested.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceExporters(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Exporters.IExporter[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newExporters
      description: The new exporters to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceExporters(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Exporters.IExporter[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newExporters
      description: The new exporters to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceLoggers(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Loggers.ILogger[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newLoggers
      description: The new loggers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceLoggers(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Loggers.ILogger[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newLoggers
      description: The new loggers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceDiagnosers(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Diagnosers.IDiagnoser[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newDiagnosers
      description: The new diagnosers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceDiagnosers(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Diagnosers.IDiagnoser[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newDiagnosers
      description: The new diagnosers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceAnalysers(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Analysers.IAnalyser[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newAnalysers
      description: The new analyzers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceAnalysers(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Analysers.IAnalyser[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newAnalysers
      description: The new analyzers to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceJobs(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Jobs.Job[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newJobs
      description: The new jobs to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceJobs(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Jobs.Job[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newJobs
      description: The new jobs to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceValidators(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Validators.IValidator[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newValidators
      description: The new validators to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceValidators(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Validators.IValidator[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newValidators
      description: The new validators to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceHardwareCounters(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Diagnosers.HardwareCounter[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newHardwareCounters
      description: The new hardware counters to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceHardwareCounters(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Diagnosers.HardwareCounter[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newHardwareCounters
      description: The new hardware counters to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceFilters(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Filters.IFilter[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newFilters
      description: The new filters to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceFilters(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Filters.IFilter[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newFilters
      description: The new filters to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceLogicalGroupRules(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newLogicalGroupRules
      description: The new logical group rules to be added.
---
---
uid: Mawosoft.Extensions.BenchmarkDotNet.ConfigExtensions.ReplaceLogicalGroupRules(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newLogicalGroupRules
      description: The new logical group rules to be added.
---
