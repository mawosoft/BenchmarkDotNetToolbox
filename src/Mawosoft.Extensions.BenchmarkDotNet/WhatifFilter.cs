// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet;

/// <summary>
/// An alternative to BenchmarkDotNet's <c>--list</c> command line option that prints a mock
/// summary of all available benchmarks according to the current effective BenchmarkDotNet configuration.
/// </summary>
public class WhatifFilter : IFilter
{
    private readonly List<BenchmarkCase> _filteredBenchmarkCases = [];
    private string[]? _consoleArguments;

    /// <summary>
    /// Gets or sets the filter's Enabled state. If <c>true</c>, the filter is enabled and will suppress
    /// the actual execution of benchmarks via <see cref="BenchmarkRunner"/> or related classes,
    /// and will collect them instead.
    /// If <c>false</c>, the filter is disabled and will not have any effect.
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
        .Select(g => new BenchmarkRunInfo([.. g], g.First().Descriptor.Type, g.First().Config));

    /// <summary>
    /// Preparses the console arguments for the option <c>--whatif</c> (short: <c>-w</c>) and automatically
    /// enables the filter if the option is present.
    /// </summary>
    /// <param name="args">The array of console arguments as passed to the <c>Main</c> method of the
    /// application.</param>
    /// <returns>The passed in array of console arguments with the <c>--whatif</c> option removed.</returns>
    public string[] PreparseConsoleArguments(string[] args)
    {
        Enabled = false;
        _consoleArguments = null;
        if (args is null)
        {
            return [];
        }
        // Get a copy of the original for summary
        _consoleArguments = (string[])args.Clone();
        // Check for -w/--whatif and remove them (including possible dupes)
        int i;
        while ((i = Array.FindIndex(args, a => a.ToLowerInvariant() is "-w" or "--whatif")) >= 0)
        {
            Enabled = true;
            args = args.Take(i).Concat(args.Skip(i + 1)).ToArray();
        }
        if (args.Length == 0)
        {
            // Don't keep a copy if empty or --whatif only
            _consoleArguments = null;
        }
        if (Enabled && Array.FindIndex(args, a => a.Equals("--list", StringComparison.OrdinalIgnoreCase)) < 0)
        {
            // Avoid BDN displaying warnings/suggestions due to all benchmarks being filtered out
            // by adding a --list option.
            Array.Resize(ref args, args.Length + 2);
            args[args.Length - 2] = "--list";
            args[args.Length - 1] = "flat";
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
        if (logger is null) throw new ArgumentNullException(nameof(logger));
        if (_filteredBenchmarkCases.Count == 0) return;

        ImmutableConfig? joinedConfig = _filteredBenchmarkCases.FirstOrDefault(bc => (bc.Config.Options & ConfigOptions.JoinSummary) != 0)?.Config;

        GenerateResult generateResult = GenerateResultWrapper.Success();
        BuildResult buildResult = BuildResult.Success(generateResult);
        IEnumerable<BenchmarkReport> reports = _filteredBenchmarkCases.Select((bc, i)
            => new BenchmarkReport(true, bc, generateResult, buildResult,
                    [
                        ExecuteResultWrapper.Create(
                            [new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, 100_000_000 + (i * 1_000_000))],
                            default, default, default)
                    ],
                    []));
        HostEnvironmentInfo hostEnvironmentInfo = HostEnvironmentInfo.GetCurrent();
        // This is the way BDN does it. SummaryStyle.CultureInfo seems to be getting ignored.
        CultureInfo cultureInfo = _filteredBenchmarkCases.First().Config.CultureInfo
                                  ?? SummaryExtensions.GetCultureInfo(null);
        ImmutableArray<ValidationError> validationErrors = ImmutableArray.Create<ValidationError>();

        logger.WriteLine();
        logger.WriteLineHeader("// * What If Summary *");
        logger.WriteLine();
        if (_consoleArguments is not null && _consoleArguments.Length != 0)
        {
            logger.WriteLineInfo("Console arguments: " + string.Join(" ", _consoleArguments));
            logger.WriteLine();
        }
        logger.WriteLineInfo(HostEnvironmentInfo.GetInformation());
        if (joinedConfig is not null)
        {
            Summary summary = SummaryWrapper.Create(
                string.Empty, reports.ToImmutableArray(), hostEnvironmentInfo,
                string.Empty, string.Empty, TimeSpan.Zero, cultureInfo, validationErrors,
                joinedConfig.GetColumnHidingRules().ToImmutableArray());
            PrintSummary(summary, logger);
        }
        else
        {
            bool useFullName =
                _filteredBenchmarkCases.Select(bc => bc.Descriptor.Type.Namespace).Distinct().Count() > 1;
            foreach (IGrouping<Type, BenchmarkReport> group in
                     reports.GroupBy(r => r.BenchmarkCase.Descriptor.Type))
            {
                Summary summary = SummaryWrapper.Create(
                    string.Empty, group.ToImmutableArray(), hostEnvironmentInfo,
                    string.Empty, string.Empty, TimeSpan.Zero, cultureInfo, validationErrors,
                    group.First().BenchmarkCase.Config.GetColumnHidingRules().ToImmutableArray());
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
    /// <see cref="IFilter"/> implementation.
    /// </summary>
    public bool Predicate(BenchmarkCase benchmarkCase)
    {
        if (benchmarkCase is null)
        {
            throw new ArgumentNullException(nameof(benchmarkCase));
        }
        // There is no guarantee that WhatifFilter is called last, so we have to check the other
        // filters if benchmarkCase passes and only collect it if it does.
        // - The dupe check is paranoid - but if we cause other filters to be called twice...
        // - BenchmarkCase only implements IComparable, not IEquatable, i.e. any lookup,
        //   HashSet, etc defaults to ReferenceEquals() - which is good.
        // - PERF (see WhatifFilterBenchmarks)
        //   The brute-force sequential lookup may seem horrible, but even for large numbers it is
        //   good enough, especially if you take jitting into account. Between Contains(), IndexOf(),
        //   and FindIndex(lambda), the latter is fastest, but allocates the most memory.
        if (Enabled
            && benchmarkCase.Config.GetFilters().Where(f => f != this).All(f => f.Predicate(benchmarkCase))
            && _filteredBenchmarkCases.IndexOf(benchmarkCase) < 0)
        {
            _filteredBenchmarkCases.Add(benchmarkCase);
        }
        return !Enabled;
    }
}
