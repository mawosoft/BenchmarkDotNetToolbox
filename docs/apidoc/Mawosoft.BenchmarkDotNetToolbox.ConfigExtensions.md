---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.IColumn[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newColumns
      description: The new columns to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Columns.IColumn[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newColumns
      description: The new columns to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.IColumnProvider[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newColumnProviders
      description: The new column providers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceColumnCategory(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Columns.IColumnProvider[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newColumnProviders
      description: The new column providers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.RemoveColumnsByCategory(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Columns.ColumnCategory[])
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
> *BenchmarkDotNetToolbox* **will not** remove the latter two column types if the removal of
> @"BenchmarkDotNet.Columns.ColumnCategory.Job?displayProperty=nameWithType" is requested.

---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.RemoveColumnsByCategory(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Columns.ColumnCategory[])
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
> *BenchmarkDotNetToolbox* **will not** remove the latter two column types if the removal of
> @"BenchmarkDotNet.Columns.ColumnCategory.Job?displayProperty=nameWithType" is requested.

---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceExporters(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Exporters.IExporter[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newExporters
      description: The new exporters to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceExporters(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Exporters.IExporter[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newExporters
      description: The new exporters to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceLoggers(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Loggers.ILogger[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newLoggers
      description: The new loggers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceLoggers(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Loggers.ILogger[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newLoggers
      description: The new loggers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceDiagnosers(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Diagnosers.IDiagnoser[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newDiagnosers
      description: The new diagnosers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceDiagnosers(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Diagnosers.IDiagnoser[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newDiagnosers
      description: The new diagnosers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceAnalysers(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Analysers.IAnalyser[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newAnalysers
      description: The new analyzers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceAnalysers(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Analysers.IAnalyser[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newAnalysers
      description: The new analyzers to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceJobs(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Jobs.Job[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newJobs
      description: The new jobs to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceJobs(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Jobs.Job[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newJobs
      description: The new jobs to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceValidators(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Validators.IValidator[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newValidators
      description: The new validators to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceValidators(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Validators.IValidator[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newValidators
      description: The new validators to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceHardwareCounters(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Diagnosers.HardwareCounter[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newHardwareCounters
      description: The new hardware counters to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceHardwareCounters(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Diagnosers.HardwareCounter[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newHardwareCounters
      description: The new hardware counters to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceFilters(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Filters.IFilter[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newFilters
      description: The new filters to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceFilters(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Filters.IFilter[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newFilters
      description: The new filters to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceLogicalGroupRules(BenchmarkDotNet.Configs.ManualConfig,BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule[])
syntax:
    parameters:
    - id: config
      description: The config to be changed.
    - id: newLogicalGroupRules
      description: The new logical group rules to be added.
---
---
uid: Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.ReplaceLogicalGroupRules(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule[])
syntax:
    parameters:
    - id: config
      description: The source config.
    - id: newLogicalGroupRules
      description: The new logical group rules to be added.
---
