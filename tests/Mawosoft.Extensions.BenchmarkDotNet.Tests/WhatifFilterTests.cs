// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;
using Xunit;
using Xunit.Abstractions;

namespace Mawosoft.Extensions.BenchmarkDotNet.Tests;

public class WhatifFilterTests
{
    private readonly ITestOutputHelper _testOutput;
    public WhatifFilterTests(ITestOutputHelper testOutput) => _testOutput = testOutput;
    public class Benchmarks1
    {
        [Benchmark(Baseline = true)]
        public int Method1() => 1;
        [Benchmark]
        public int Method2() => 2;
        [Benchmark]
        public int Method3() => 3;
    }

    public class Benchmarks2
    {
        [ParamsAllValues]
        public bool Prop1 { get; set; }
        [Benchmark]
        public int Method1() => Prop1 ? 10 : 11;
        [Benchmark]
        public int Method2() => Prop1 ? 20 : 21;
        [Benchmark]
        [Arguments(44)]
        public int Method3(int next) => Prop1 ? 30 : 31 + next;
    }

    [DryJob]
    public class Benchmarks3
    {
        [Benchmark]
        public int Method1() => 100;
        [Benchmark]
        public int Method2() => 200;
    }

    private class MockFilter : IFilter
    {
        public bool ShouldFilter { get; set; }
        public MockFilter(bool shouldFilter = true)
        {
            ShouldFilter = shouldFilter;
        }
        public bool Predicate(BenchmarkCase benchmarkCase) => !ShouldFilter;
    }

    [Fact]
    public void Enabled_Roundtrip()
    {
        WhatifFilter whatifFilter = new();
        Assert.False(whatifFilter.Enabled);
        whatifFilter.Enabled = true;
        Assert.True(whatifFilter.Enabled);
        whatifFilter.Enabled = false;
        Assert.False(whatifFilter.Enabled);
    }

    [Theory]
    #region InlineData
    [InlineData("--some args --whatif", "--some args --list flat", true)]
    [InlineData("--some args -w", "--some args --list flat", true)]
    [InlineData("--some args --wHATiF", "--some args --list flat", true)]
    [InlineData("--some args -w --other", "--some args --other --list flat", true)]
    [InlineData("--some args -w --other --whatif", "--some args --other --list flat", true)]
    [InlineData("-f * --list tree --whatif", "-f * --list tree", true)]
    [InlineData("-f * --foo --some args", null, false)]
    [InlineData("", "", false)]
    [InlineData(null, "", false)]
    #endregion
    public void PreparseConsoleArguments_Succeeds(string? arguments, string? expectedPreparsed, bool expectedEnabled)
    {
        string[]? args = arguments?.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        string[] expected = expectedPreparsed?.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            ?? (string[]?)args?.Clone() ?? Array.Empty<string>();
        WhatifFilter whatifFilter = new();
        args = whatifFilter.PreparseConsoleArguments(args!);
        Assert.Equal(expectedEnabled, whatifFilter.Enabled);
        Assert.Equal(expected, args);
    }

    [Fact]
    public void Predicate_NullArgument_Throws()
    {
        WhatifFilter whatifFilter = new();
        Assert.Throws<ArgumentNullException>("benchmarkCase", () => whatifFilter.Predicate(null!));
        whatifFilter.Enabled = true;
        Assert.Throws<ArgumentNullException>("benchmarkCase", () => whatifFilter.Predicate(null!));
    }

    [Fact]
    public void Predicate_IfNotEnabled_DoesntFilter()
    {
        WhatifFilter whatifFilter = new();
        bool result = whatifFilter.Predicate(BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks1)).BenchmarksCases[0]);
        Assert.True(result);
        Assert.Empty(whatifFilter.FilteredBenchmarkCases);
    }

    [Fact]
    public void Predicate_WithOtherFilters()
    {
        WhatifFilter whatifFilter = new();
        whatifFilter.Enabled = true;
        MockFilter mockFilter = new(shouldFilter: true);
        ManualConfig config = ManualConfig.CreateEmpty().AddFilter(mockFilter, whatifFilter);
        BenchmarkRunInfo runInfo = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks1));
        BenchmarkCase benchmarkCase = runInfo.BenchmarksCases[0];
        benchmarkCase = BenchmarkCase.Create(benchmarkCase.Descriptor, benchmarkCase.Job,
            benchmarkCase.Parameters, ImmutableConfigBuilder.Create(config));

        bool result = whatifFilter.Predicate(benchmarkCase);
        Assert.False(result);
        Assert.Empty(whatifFilter.FilteredBenchmarkCases);
        mockFilter.ShouldFilter = false;
        result = whatifFilter.Predicate(benchmarkCase);
        Assert.False(result);
        Assert.Single(whatifFilter.FilteredBenchmarkCases);
    }

    [Fact]
    public void Predicate_IgnoresDuplicates()
    {
        WhatifFilter whatifFilter = new();
        whatifFilter.Enabled = true;
        BenchmarkCase benchmarkCase = BenchmarkConverter.TypeToBenchmarks(typeof(Benchmarks1)).BenchmarksCases[0];
        bool result = whatifFilter.Predicate(benchmarkCase);
        Assert.False(result);
        Assert.Single(whatifFilter.FilteredBenchmarkCases);
        result = whatifFilter.Predicate(benchmarkCase);
        Assert.False(result);
        Assert.Single(whatifFilter.FilteredBenchmarkCases);
    }

    [Fact]
    public void FilteredBenchmarkCases_EqualsConvertedBenchmarks()
    {
        Type[] types = new[] { typeof(Benchmarks1), typeof(Benchmarks2), typeof(Benchmarks3) };
        WhatifFilter whatifFilter = new();
        ManualConfig config = ManualConfig.CreateEmpty().AddFilter(whatifFilter);
        // Do *NOT* allow deferred execution here, otherwise expected will be empty
        BenchmarkCase[] expected = types
            .SelectMany(t => BenchmarkConverter.TypeToBenchmarks(t, config).BenchmarksCases)
            .OrderBy(b => b.DisplayInfo)
            .ToArray();
        // Run again with filter enabled
        whatifFilter.Enabled = true;
        Assert.Empty(types.SelectMany(t => BenchmarkConverter.TypeToBenchmarks(t, config).BenchmarksCases));
        Assert.Equal(expected, whatifFilter.FilteredBenchmarkCases.OrderBy(b => b.DisplayInfo));
        Assert.Equal(expected,
                     whatifFilter.FilteredBenchmarkRunInfos.SelectMany(r => r.BenchmarksCases)
                                                           .OrderBy(b => b.DisplayInfo));
    }

    // TODO More detailed tests
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PrintAsSummaries_Succeeds(bool join)
    {
        AccumulationLogger logger = new(); // This is text-only. Use LogCapture if LogKind is needed.
        Type[] types = new[] { typeof(Benchmarks1), typeof(Benchmarks2), typeof(Benchmarks3) };
        WhatifFilter whatifFilter = new();
        whatifFilter.Enabled = true;
        ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
            .ReplaceLoggers(logger)
            .AddFilter(whatifFilter)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithOption(ConfigOptions.JoinSummary, join);
        // Do *NOT* allow deferred execution here
        _ = types.Select(t => BenchmarkConverter.TypeToBenchmarks(t, config)).ToArray();
        whatifFilter.PrintAsSummaries(logger);
        _testOutput.WriteLine(logger.GetLog());
    }
}
