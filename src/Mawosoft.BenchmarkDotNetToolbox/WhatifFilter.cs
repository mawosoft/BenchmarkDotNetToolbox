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
    // TODO inline doc
    public class WhatifFilter : IFilter, IDisposable
    {
        private readonly List<BenchmarkCase> _filteredBenchmarkCases = new();
        public bool Enabled { get; set; }
        public IEnumerable<BenchmarkCase> FilteredBenchmarkCases => _filteredBenchmarkCases;
        public IEnumerable<BenchmarkRunInfo> FilteredBenchmarkRunInfos => _filteredBenchmarkCases
            .GroupBy(bc => bc.Descriptor.Type)
            .Select(g => new BenchmarkRunInfo(g.ToArray(), g.First().Descriptor.Type, g.First().Config));

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

        public void Clear(bool dispose)
        {
            if (dispose)
            {
                _filteredBenchmarkCases.ForEach(bc => bc.Dispose());
            }
            _filteredBenchmarkCases.Clear();
        }

        public void PrintAsSummaries(ILogger logger, bool? joinTristate)
        {
            bool join = joinTristate
                        ?? _filteredBenchmarkCases.Any(bc => (bc.Config.Options & ConfigOptions.JoinSummary) != 0);

            GenerateResult generateResult = GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>());
            BuildResult buildResult = BuildResult.Success(generateResult);
            IEnumerable<BenchmarkReport> reports = _filteredBenchmarkCases.Select((bc, i) => new BenchmarkReport(
                true, bc, generateResult, buildResult, Array.Empty<ExecuteResult>(),
                new[] { new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1,
                                        100_000_000 + i * 1_000_000) },
                default, Array.Empty<Metric>()));

            HostEnvironmentInfo hostEnvironmentInfo = HostEnvironmentInfo.GetCurrent();
            CultureInfo cultureInfo = _filteredBenchmarkCases.FirstOrDefault()?.Config.CultureInfo
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

        public bool Predicate(BenchmarkCase benchmarkCase)
        {
            if (Enabled)
            {
                _filteredBenchmarkCases.Add(benchmarkCase);
            }
            return !Enabled;
        }

        public void Dispose() => Clear(dispose: true);
    }
}
