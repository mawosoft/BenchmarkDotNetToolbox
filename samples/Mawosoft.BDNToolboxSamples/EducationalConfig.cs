// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Validators;
using Mawosoft.BenchmarkDotNetToolbox;

namespace Mawosoft.BDNToolboxSamples
{
    /// <summary>
    /// This class isn't really used anywhere, it exists just for (self-)educational purposes.
    /// </summary>
    public static class EducationalConfig
    {
        // Verbose recreation of DefaultConfig.
        // Gives an overview of possible customizations.
        public static ManualConfig CreateVerboseDefaultConfig()
        {
            ManualConfig config = ManualConfig.CreateEmpty()
                // DefaultColumnProviders.Instance:
                .AddColumnProvider(
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Job,
                    DefaultColumnProviders.Statistics,
                    DefaultColumnProviders.Params,
                    DefaultColumnProviders.Metrics
                    )
                .AddExporter(
                    CsvExporter.Default,
                    MarkdownExporter.GitHub,
                    HtmlExporter.Default
                    )
                // If available DefaultConfig will add LinqPadLogger.Instance instead.
                .AddLogger(
                    ConsoleLogger.Default
                    )
                // No default diagnosers
                .AddDiagnoser()
                .AddAnalyser(
                    EnvironmentAnalyser.Default,
                    OutliersAnalyser.Default,
                    MinIterationTimeAnalyser.Default,
                    MultimodalDistributionAnalyzer.Default,
                    RuntimeErrorAnalyser.Default,
                    ZeroMeasurementAnalyser.Default,
                    BaselineCustomAnalyzer.Default
                    )
                // No defaults. If none, ImmutableConfigBuilder will add Job.Default
                .AddJob()
                // The validators in DefaultConfig are slightly different from the mandatory ones
                // added by ImmutableConfigBuilder.
                .AddValidator(
                    BaselineValidator.FailOnError,               // mandatory
                    SetupCleanupValidator.FailOnError,           // mandatory
                    JitOptimizationsValidator.FailOnError,       // mandatory: DontFailOnError
                    RunModeValidator.FailOnError,                // mandatory
                    GenericBenchmarksValidator.DontFailOnError,
                    DeferredExecutionValidator.FailOnError,      // mandatory: DontFailOnError
                    ParamsAllValuesValidator.FailOnError
                    )
                // When ImmutableConfigBuilder finalizes the config the following mandatory validators
                // will be *always* added. With dupes, FailOnError has precedence.
                .AddValidator(
                    BaselineValidator.FailOnError,               // default
                    SetupCleanupValidator.FailOnError,           // default
                    RunModeValidator.FailOnError,                // default
                    DiagnosersValidator.Composite,
                    CompilationValidator.FailOnError,
                    ConfigValidator.DontFailOnError,
                    ShadowCopyValidator.DontFailOnError,
                    JitOptimizationsValidator.DontFailOnError,   // default: FailOnError
                    DeferredExecutionValidator.DontFailOnError   // default: FailOnError
                    )
                // No defaults
                .AddHardwareCounters()
                .AddFilter()
                .AddLogicalGroupRules();
            // If null, ImmutableConfigBuilder will use DefaultOrderer.Instance, which is
            //   new DefaultOrderer(SummaryOrderPolicy.Default, MethodOrderPolicy.Declared);
            // where SummaryOrderPolicy.Default seems to be execution order.(?)
            config.Orderer = null;
            // SummaryStyle.Default which has:
            //   PrintUnitsInHeader = false, PrintUnitsInContent = true, PrintZeroValuesInContent = false,
            //   MaxParameterColumnWidth = 20, SizeUnit = null, TimeUnit = null,
            //   CultureInfo = CultureInfo.InvariantCulture with explicit NumberDecimalSeparator = ".",
            //   RatioStyle = RatioStyle.Value
            config.SummaryStyle = SummaryStyle.Default;
            config.UnionRule = ConfigUnionRule.Union;
            // OS dependent. If null, ImmutableConfigBuilder will use DefaultConfig.Instance.ArtifactsPath
            config.ArtifactsPath = DefaultConfig.Instance.ArtifactsPath;
            config.CultureInfo = null;
            config.Options = ConfigOptions.Default;
            return config;
        }

        // Minimal steps to create default config from scratch.
        // Intended as a template for creating custom configs with BDN's on-board methods.
        public static ManualConfig CreateTerseDefaultConfig(bool preferMandatoryValidators)
        {
            ManualConfig config = ManualConfig.CreateEmpty()
                .AddColumnProvider(DefaultColumnProviders.Instance)
                .AddExporter(DefaultConfig.Instance.GetExporters().ToArray())
                .AddLogger(ConsoleLogger.Default)
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray())
                .AddValidator(preferMandatoryValidators
                    ? new[] { GenericBenchmarksValidator.DontFailOnError, ParamsAllValuesValidator.FailOnError }
                    : DefaultConfig.Instance.GetValidators().ToArray()
                    );
            return config;
        }

        // Minimal steps to create a custom config from scratch. In this custom config we want:
        // - custom column providers for job and param columns
        // - Only the Mean and RatioMean columns instead of all statistic columns
        // - Only the MarkdownExporter.Console
        // - Only the ConsoleLogger.Unicode instead of the default, non-Unicode one.
        // - Everything else should be like DefaultConfig.
        public static ManualConfig CreateCustomConfigFromScratch()
        {
            ManualConfig config = ManualConfig.CreateEmpty()
                .AddColumnProvider(
                    DefaultColumnProviders.Descriptor,
                    new JobColumnSelectionProvider("-all +Job", showHiddenValuesInLegend: true),
                    new RecyclableParamsColumnProvider(),
                    DefaultColumnProviders.Metrics
                    )
                .AddColumn(StatisticColumn.Mean, BaselineRatioColumn.RatioMean)
                .AddExporter(MarkdownExporter.Console)
                .AddLogger(ConsoleLogger.Unicode)
                .AddAnalyser(DefaultConfig.Instance.GetAnalysers().ToArray())
                .AddValidator(DefaultConfig.Instance.GetValidators().ToArray());
            return config;
        }

        // The same custom config as above, but created by starting with DefaultConfig and using
        // the Mawosoft.BenchmarkDotNetToolbox.ManualConfigExtensions methods (Replacers)
        // This doesn't actually seem like less work, but...
        // - If you really want to make that many changes, you should probably declare a custom
        //   config anyway.
        // - Main purpose of Replacer methods are little changes from Default or a given config.
        // - Even with many changes, you still only have to think about the changes, not which
        //   defaults you may have forgotten to add.
        public static ManualConfig CreateCustomConfigFromDefaultWithNewExtensionMethods()
        {
            ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
                .ReplaceColumnCategory(ColumnCategory.Job,
                    new JobColumnSelectionProvider("-all +Job", showHiddenValuesInLegend: true))
                .ReplaceColumnCategory(ColumnCategory.Params,
                    new RecyclableParamsColumnProvider())
                .ReplaceColumnCategory(StatisticColumn.Mean, BaselineRatioColumn.RatioMean)
                .ReplaceExporters(MarkdownExporter.Console)
                .ReplaceLoggers(ConsoleLogger.Unicode);
            return config;
        }
    }
}
