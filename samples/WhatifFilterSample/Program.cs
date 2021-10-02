// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Mawosoft.BenchmarkDotNetToolbox;

namespace WhatifFilterSample
{
    public class Benchmarks1
    {
        [Benchmark(Baseline = true)]
        public int Method1() => new Random().Next(10);
        [Benchmark]
        public int Method2() => new Random().Next(20) + new Random().Next(21);
        [Benchmark]
        public int Method3() => new Random().Next(30);
    }

    public class Benchmarks2
    {
        [ParamsAllValues]
        public bool Prop1 { get; set; }
        [Benchmark]
        public int Method1() => new Random().Next(Prop1 ? 40 : 41);
        [Benchmark]
        public int Method2() => new Random().Next(Prop1 ? 42 : 43);
        [Benchmark]
        [Arguments(44)]
        public int Method3(int next) => new Random().Next(next);
    }

    [DryJob]
    public class Benchmarks3
    {
        [Benchmark]
        public int Method1() => new Random().Next(50);
        [Benchmark]
        public int Method2() => new Random().Next(60);
    }

    internal class Program
    {
        public static void Main(string[] args)
        {
            _ = args; // make it not 'unused'

            // Sample command line
            // If you want to use actual command line arguments, comment this line out.

            args = "--runtimes net5.0 netcoreapp31 net48 --whatif".Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Create the WhatifFilter and let it process the --whatif (or -w for short) argument if one exists.
            // If the argument is present, the filter is enabled and the argument removed from the returned args,
            // so BDN won't complain about invalid arguments.
            // If the --whatif argument is not present, the filter will have no effect at all.
            // Alternatively, you can also enable or disable the filter by setting its Enabled property.

            WhatifFilter whatifFilter = new();
            args = whatifFilter.PreparseConsoleArguments(args);

            // Create a global confing and add the filter.
            // We are using the DefaultConfig here for simplicity, but add a ShortRun job as default
            // to have some common columns to display.

            ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
                .AddFilter(whatifFilter)
                .AddJob(Job.ShortRun.AsDefault().UnfreezeCopy())
                .WithOptions(ConfigOptions.DisableOptimizationsValidator);

            // Run your benchmarks. If the filter was enabled, the returned array will be empty.

            Summary[] summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);
            _ = summaries;

            // Check the filter and print the What-if summaries

            if (whatifFilter.Enabled)
            {
                whatifFilter.PrintAsSummaries(ConsoleLogger.Default);
                whatifFilter.Clear(dispose: true);
            }
        }
    }
}
