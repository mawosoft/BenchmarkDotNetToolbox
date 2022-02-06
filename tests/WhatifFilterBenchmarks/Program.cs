// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Mawosoft.Extensions.BenchmarkDotNet;
using Perfolizer.Horology;

namespace WhatifFilterBenchmarks
{
    public class ListVsHashSetBenchmarks
    {
        private const int MaxBenchmarks = 5000;
        private List<BenchmarkCase> _sourceBenchmarks = new();

        [GlobalSetup]
        public void GlobalSetup()
        {
            GlobalCleanup(); // Paranoid
            _sourceBenchmarks = new(MaxBenchmarks);
            // BenchmarkCase only implements IComparable, not IEquatable.
            // Any Equals() operation thus will be ReferenceEquals() by default.
            BenchmarkCase bcBase = BenchmarkCase.Create(
                new Descriptor(
                    typeof(ListVsHashSetBenchmarks),
                    typeof(ListVsHashSetBenchmarks).GetMethod(nameof(ListVsHashSetBenchmarks.ListPlusHashSet_ExplicitComparer))),
                Job.Default, new ParameterInstances(Array.Empty<ParameterInstance>()),
                DefaultConfig.Instance.CreateImmutableConfig());
            for (int i = 0; i < MaxBenchmarks; i++)
            {
                _sourceBenchmarks.Add(BenchmarkCase.Create(bcBase.Descriptor, bcBase.Job, bcBase.Parameters, bcBase.Config));
            }
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            _sourceBenchmarks.ForEach(b => b.Dispose());
            _sourceBenchmarks.Clear();
        }

        public IEnumerable<int> ArgumentsSource()
        {
            yield return 10;
            yield return 100;
            yield return 500;
            yield return 1000;
        }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(ArgumentsSource))]
        public void ListPlusHashSet_ExplicitComparer(int count)
        {
            List<BenchmarkCase> list = new();
            HashSet<BenchmarkCase> hashset = new(ReferenceEqualityComparer.Instance);
            for (int i = 0; i < count; i++)
            {
                BenchmarkCase bc = _sourceBenchmarks[i];
                if (hashset.Add(bc))
                {
                    list.Add(bc);
                }
            }
            if (list.Count != count)
            {
                throw new InvalidOperationException("List<>.Count != count");
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ArgumentsSource))]
        public void ListPlusHashSet_ImplicitComparer(int count)
        {
            List<BenchmarkCase> list = new();
            HashSet<BenchmarkCase> hashset = new();
            for (int i = 0; i < count; i++)
            {
                BenchmarkCase bc = _sourceBenchmarks[i];
                if (hashset.Add(bc))
                {
                    list.Add(bc);
                }
            }
            if (list.Count != count)
            {
                throw new InvalidOperationException("List<>.Count != count");
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ArgumentsSource))]
        public void ListContains_ExplicitComparer(int count)
        {
            List<BenchmarkCase> list = new();
            for (int i = 0; i < count; i++)
            {
                BenchmarkCase bc = _sourceBenchmarks[i];
                if (!list.Contains(bc, ReferenceEqualityComparer.Instance))
                {
                    list.Add(bc);
                }
            }
            if (list.Count != count)
            {
                throw new InvalidOperationException("List<>.Count != count");
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ArgumentsSource))]
        public void ListContains_ImplicitComparer(int count)
        {
            List<BenchmarkCase> list = new();
            for (int i = 0; i < count; i++)
            {
                BenchmarkCase bc = _sourceBenchmarks[i];
                if (!list.Contains(bc))
                {
                    list.Add(bc);
                }
            }
            if (list.Count != count)
            {
                throw new InvalidOperationException("List<>.Count != count");
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ArgumentsSource))]
        public void ListIndexOf_ImplicitComparer(int count)
        {
            List<BenchmarkCase> list = new();
            for (int i = 0; i < count; i++)
            {
                BenchmarkCase bc = _sourceBenchmarks[i];
                if (list.IndexOf(bc) < 0)
                {
                    list.Add(bc);
                }
            }
            if (list.Count != count)
            {
                throw new InvalidOperationException("List<>.Count != count");
            }
        }

        [Benchmark]
        [ArgumentsSource(nameof(ArgumentsSource))]
        public void ListFindIndex_LambdaRefEquals(int count)
        {
            List<BenchmarkCase> list = new();
            for (int i = 0; i < count; i++)
            {
                BenchmarkCase bc = _sourceBenchmarks[i];
                if (list.FindIndex(b => ReferenceEquals(b, bc)) < 0)
                {
                    list.Add(bc);
                }
            }
            if (list.Count != count)
            {
                throw new InvalidOperationException("List<>.Count != count");
            }
        }
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            WhatifFilter whatifFilter = new();
            args = whatifFilter.PreparseConsoleArguments(args);

            ManualConfig config = DefaultConfig.Instance
                .ReplaceColumnCategory(new JobColumnSelectionProvider("-all +Job"))
                // We don't need the individual "Measurements", so JsonExporter.Brief would be sufficient,
                // except that it also excludes "Metrics" (memory allocation in this case).
                .ReplaceExporters(MarkdownExporter.Console, JsonExporter.FullCompressed)
                .ReplaceLoggers(ConsoleLogger.Unicode)
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig(displayGenColumns: false)))
                .AddFilter(whatifFilter)
                .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Microsecond))
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true);

            Summary[] summaries;
            if (!whatifFilter.Enabled && args.Length == 0 && Debugger.IsAttached)
            {
                BenchmarkRunInfos runInfos = new(config, BenchmarkRunInfos.FastInProcessJob);
                runInfos.ConvertAssemblyToBenchmarks(typeof(Program).Assembly);
                summaries = runInfos.RunAll();
            }
            else
            {
                summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);
            }
            _ = summaries;

            if (whatifFilter.Enabled)
            {
                whatifFilter.PrintAsSummaries(ConsoleLogger.Unicode);
                whatifFilter.Clear(dispose: true);
            }
        }
    }
}
