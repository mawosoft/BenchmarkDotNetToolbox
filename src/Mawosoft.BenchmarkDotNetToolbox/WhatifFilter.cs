// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>
    /// This filter allows you to see results of a particular BechmarkDotnet configuration without actually
    /// running the benchmarks.
    /// </summary>
    /// <remarks>
    /// <para>The term 'configuration' here refers to the interplay of global and local configs, attribute
    /// annotations, and console arguments. The filter can be controlled programmatically with the
    /// <see cref="Enabled"/> property or by using <see cref="PreparseConsoleArguments"/> to scan the command
    /// line for the option <b>--whatif</b> (short: <b>-w</b>). </para>
    /// <para>If enabled, the filter will collect all benchmark cases produced by <see cref="BenchmarkRunner"/>,
    /// <see cref="BenchmarkSwitcher"/> or <see cref="BenchmarkConverter"/> and suppress their actual execution.
    /// </para>
    /// <para>The properties <see cref="FilteredBenchmarkCases"/> and <see cref="FilteredBenchmarkRunInfos"/>
    /// can be used to access the collected benchmarks cases.</para>
    /// <para>With <see cref="PrintAsSummaries"/> you can output them to the console or another logger.</para>
    /// 
    /// </remarks>
    public class WhatifFilter : IFilter, IDisposable
    {
        private readonly List<BenchmarkCase> _filteredBenchmarkCases = new();
        /// <summary>
        /// Gets or sets the filter's Enabled state. When true, the filter will suppress the actual execution
        /// of benchmarks via <see cref="BenchmarkRunner"/> or related classes, and will collect them instead.
        /// When false, the filter will not have any effect.
        /// </summary>
        public bool Enabled { get; set; }
        /// <summary>
        /// Gets an enumerable collection of the filtered, individual benchmark cases.
        /// </summary>
        public IEnumerable<BenchmarkCase> FilteredBenchmarkCases => _filteredBenchmarkCases;
        /// <summary>
        /// Gets an enumerable collection of the filtered benchmark cases grouped by containing type.
        /// </summary>
        public IEnumerable<BenchmarkRunInfo> FilteredBenchmarkRunInfos => _filteredBenchmarkCases
            .GroupBy(bc => bc.Descriptor.Type)
            .Select(g => new BenchmarkRunInfo(g.ToArray(), g.First().Descriptor.Type, g.First().Config));

        /// <summary>
        /// Preparses the console arguments for the option <b>--whatif</b> (short: <b>-w</b>) and automatically
        /// enables the filter if the option is present.
        /// </summary>
        /// <param name="args">The array of console arguments as passed to the <b>Main</b> method of your
        /// application.</param>
        /// <returns>The passed in array of console arguments with the <b>--whatif</b> option removed.</returns>
        public string[] PreparseConsoleArguments(string[] args)
        {
            Enabled = false;
            if (args == null)
            {
                return Array.Empty<string>();
            }
            int i = Array.FindIndex(args, a => a.ToLowerInvariant() is "-w" or "--whatif");
            if (i >= 0)
            {
                Enabled = true;
                args = args.Take(i).Concat(args.Skip(i + 1)).ToArray();
            }
            return args;
        }

        /// <summary>
        /// Clears the list of collected benchmark cases and optionally disposes them.
        /// </summary>
        public void Clear(bool dispose)
        {
            if (dispose)
            {
                _filteredBenchmarkCases.ForEach(bc => bc.Dispose());
            }
            _filteredBenchmarkCases.Clear();
        }

        /// <summary>
        /// Prints the collected benchmark cases as summaries to the given logger.
        /// </summary>
        public void PrintAsSummaries(ILogger logger)
        {
            if (_filteredBenchmarkCases.Count == 0)
                return;

            bool join = _filteredBenchmarkCases.Any(bc => (bc.Config.Options & ConfigOptions.JoinSummary) != 0);

            GenerateResult generateResult = GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>());
            BuildResult buildResult = BuildResult.Success(generateResult);
            IEnumerable<BenchmarkReport> reports = _filteredBenchmarkCases.Select((bc, i) => new BenchmarkReport(
                true, bc, generateResult, buildResult, Array.Empty<ExecuteResult>(),
                new[] { new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1,
                                        100_000_000 + i * 1_000_000) },
                default, Array.Empty<Metric>()));

            HostEnvironmentInfo hostEnvironmentInfo = HostEnvironmentInfo.GetCurrent();
            // This is the way BDN does it. SummaryStyle.CultureInfo seems to be getting ignored.
            CultureInfo cultureInfo = _filteredBenchmarkCases.First().Config.CultureInfo
                                      ?? SummaryExtensions.GetCultureInfo(null);
            ImmutableArray<ValidationError> validationErrors = ImmutableArray.Create<ValidationError>();

            logger.WriteLine();
            logger.WriteLineHeader("// * What If Summary *");
            logger.WriteLine();
            logger.WriteLineInfo(HostEnvironmentInfo.GetInformation());
            if (join)
            {
                Summary summary = new(string.Empty, reports.ToImmutableArray(), hostEnvironmentInfo,
                                      string.Empty, string.Empty, TimeSpan.Zero, cultureInfo, validationErrors);
                PrintSummary(summary, logger);
            }
            else
            {
                bool useFullName =
                    _filteredBenchmarkCases.Select(bc => bc.Descriptor.Type.Namespace).Distinct().Count() > 1;
                foreach (IGrouping<Type, BenchmarkReport> group in
                         reports.GroupBy(r => r.BenchmarkCase.Descriptor.Type))
                {
                    Summary summary = new(string.Empty, group.ToImmutableArray(), hostEnvironmentInfo,
                                          string.Empty, string.Empty, TimeSpan.Zero, cultureInfo, validationErrors);
                    logger.WriteLine();
                    logger.WriteLineHeader($"// {(useFullName ? group.Key.FullName : group.Key.Name)}");
                    logger.WriteLine();
                    PrintSummary(summary, logger);
                }
            }

            static void PrintSummary(Summary summary, ILogger logger)
            {
                summary.Table.PrintCommonColumns(logger);
                logger.WriteLine();
                LogCapture capture = new();
                MarkdownExporter.Console.ExportToLog(summary, capture);
                List<OutputLine> lines =
                    capture.CapturedOutput.SkipWhile(ol => ol.Kind != LogKind.Statistic).ToList();
                lines.ForEach(ol => logger.Write(ol.Kind, ol.Text));
            }
        }

        /// <summary>
        /// Called internally by <see cref="BenchmarkRunner"/> and related classes.
        /// </summary>
        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            if (Enabled)
            {
                _filteredBenchmarkCases.Add(benchmarkCase);
            }
            return !Enabled;
        }

        /// <summary>
        /// Clears and disposes the collected benchmark cases.
        /// </summary>
        public void Dispose() => Clear(dispose: true);
    }
}
