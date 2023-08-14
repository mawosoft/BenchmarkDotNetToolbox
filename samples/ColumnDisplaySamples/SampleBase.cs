// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace ColumnDisplaySamples;

public abstract class SampleBase
{
    protected static readonly string BenchmarkDotNetDefault = "with BenchmarkDotNet defaults";
    internal abstract string SampleGroupDescription { get; }
    internal abstract string SampleVariantDescription { get; }

    internal class SampleConfigBase : ManualConfig
    {
        // Our default console logger
        public static readonly ILogger Console = ConsoleLogger.Unicode;
        public SampleConfigBase() : base()
        {
            AddColumnProvider(DefaultColumnProviders.Instance);
            AddExporter(MarkdownExporter.Console);
            // Our console logger, plus LogCapture for creating sample overview section
            AddLogger(Console, LogParser.LogCapture);
            // We run a lot of Dry and InProcess jobs, possibly in DEBUG mode, and don't want
            // these analysers to complain every time.
            AddAnalyser(DefaultConfig.Instance.GetAnalysers()
                .Where(a => a != EnvironmentAnalyser.Default && a != MinIterationTimeAnalyser.Default)
                .ToArray());
            AddValidator(DefaultConfig.Instance.GetValidators().ToArray());
            SummaryStyle = SummaryStyle.Default;
            UnionRule = ConfigUnionRule.AlwaysUseLocal;
            Options = ConfigOptions.DisableOptimizationsValidator;
        }
    }
}
