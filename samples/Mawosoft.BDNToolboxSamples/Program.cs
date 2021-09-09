// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using BenchmarkDotNet.Analysers;
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
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using Mawosoft.BenchmarkDotNetToolbox;

// Not using Mawosoft.BenchmarkDotNetToolbox.Samples on purpose.
// We want the "using Mawosoft.BenchmarkDotNetToolbox;" directive to be required in the samples.
namespace Mawosoft.BDNToolboxSamples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // TODO Use a filter to run BenchmarkRunInfos samples separately if they are selected
            var switcher = BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly);
            var config = new SampleBase.SampleConfigBase();
            List<Summary> summaries = switcher.Run(args, config).ToList();
            _ = summaries;
        }
    }
}
// TODO repeat all summary tables with legends at the end.
// - Use SampleBase.SampleGroupDescription/SampleVariantDescription as headlines
// - Use a compositelogger ConsoleLogger.Unicode + StreamLogger(logfilePath, append: true);
// - use a custom exporter like MarkdownExporter.Console, but add legend.
//   For legends, see https://github.com/dotnet/BenchmarkDotNet/blob/141ef7421496b68ded18710869509ca9c76414ec/src/BenchmarkDotNet/Running/BenchmarkRunnerClean.cs#L240-L259
