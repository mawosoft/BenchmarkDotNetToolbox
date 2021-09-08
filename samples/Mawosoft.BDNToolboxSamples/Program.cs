// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using Mawosoft.BenchmarkDotNetToolbox;

namespace Mawosoft.BDNToolboxSamples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ManualConfig config;
            Summary summary;

            // With BDN's default Job column display
            config = ManualConfig.Create(DefaultConfig.Instance)
                .ReplaceExporters(MarkdownExporter.Console)
                .ReplaceLoggers(ConsoleLogger.Unicode)
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                .WithOption(ConfigOptions.DontOverwriteResults, true)
                .AddJob(
                    Job.Dry.WithToolchain(InProcessEmitToolchain.DontLogOutput),
                    Job.Default.WithToolchain(InProcessEmitToolchain.DontLogOutput)
                    );
            summary = BenchmarkRunner.Run(
                typeof(JobColumnSampleBenchmarks.UsingGlobalConfigForEverything),
                config, args);

            // Same config as above, but now with JobColumnSelectionProvider
            config.ReplaceColumnCategory(ColumnCategory.Job,
                new JobColumnSelectionProvider("-all +Job", showHiddenValuesInLegend: true));
            _ = summary;
            summary = BenchmarkRunner.Run(
                typeof(JobColumnSampleBenchmarks.UsingGlobalConfigForEverything),
                config, args);
            _ = summary;
        }
    }
}
