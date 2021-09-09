// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Linq;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;

namespace Mawosoft.BDNToolboxSamples
{
    public abstract class SampleBase
    {
        protected static readonly string BenchmarkDotNetDefault = "with BenchmarkDotNet defaults";
        public abstract string SampleGroupDescription { get; }
        public abstract string SampleVariantDescription { get; }
        public class SampleConfigBase : ManualConfig
        {
            public SampleConfigBase() : base()
            {
                AddColumnProvider(DefaultColumnProviders.Instance);
                AddExporter(MarkdownExporter.Console);
                AddLogger(ConsoleLogger.Unicode);
                AddAnalyser(DefaultConfig.Instance.GetAnalysers()
                    .Where(a => a != EnvironmentAnalyser.Default && a != MinIterationTimeAnalyser.Default)
                    .ToArray());
                AddValidator(DefaultConfig.Instance.GetValidators().ToArray());
                WithOption(ConfigOptions.DisableOptimizationsValidator, true);
                UnionRule = ConfigUnionRule.AlwaysUseLocal;
            }
        }
    }
}
