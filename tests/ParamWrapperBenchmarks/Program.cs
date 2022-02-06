// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Diagnostics;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Mawosoft.Extensions.BenchmarkDotNet;

namespace ParamWrapperBenchmarks
{
    // Perf note: The property getter gets inlined and optimized after a while, but initially it's a method call,
    // even with MethodImplOptions.AggressiveInlining
    public class ParamWrapperWithValueAsProperty<T>
    {
        public T Value { get; set; }
        public string? DisplayText { get; set; }
        public override string? ToString() => DisplayText ?? Value?.ToString() ?? "<null>";
        public ParamWrapperWithValueAsProperty(T value, string? displayText)
        {
            Value = value;
            DisplayText = displayText;
        }
    }

    [DryJob]
    [SimpleJob]
    public class ParamWrapperBenchmarks
    {
        private const string IntParamDisplayText = "1";
        private const string StructParamDisplayText = "2";
        private const string ClassParamDisplayText = "3";
        public IEnumerable<int> IntDirectArgument() { yield return 1; }
        public IEnumerable<ParamWrapper<int>> IntFieldWrappedArgument() { yield return new ParamWrapper<int>(1, IntParamDisplayText); }
        public IEnumerable<ParamWrapperWithValueAsProperty<int>> IntPropertyWrappedArgument() { yield return new ParamWrapperWithValueAsProperty<int>(1, IntParamDisplayText); }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(IntDirectArgument))]
        public int IntDirect(int p1) => p1;
        [Benchmark]
        [ArgumentsSource(nameof(IntFieldWrappedArgument))]
        public int IntFieldWrapped(ParamWrapper<int> p1) => p1.Value;
        [Benchmark]
        [ArgumentsSource(nameof(IntPropertyWrappedArgument))]
        public int IntPropertyWrapped(ParamWrapperWithValueAsProperty<int> p1) => p1.Value;

        public struct S1 { public int Int1; public int Int2; public int Int3; public override string ToString() => StructParamDisplayText; }
        public IEnumerable<S1> StructDirectArgument() { yield return new S1(); }
        public IEnumerable<ParamWrapper<S1>> StructFieldWrappedArgument() { yield return new ParamWrapper<S1>(new S1(), StructParamDisplayText); }
        public IEnumerable<ParamWrapperWithValueAsProperty<S1>> StructPropertyWrappedArgument() { yield return new ParamWrapperWithValueAsProperty<S1>(new S1(), StructParamDisplayText); }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(StructDirectArgument))]
        public S1 StructDirect(S1 p1) => p1;
        [Benchmark]
        [ArgumentsSource(nameof(StructFieldWrappedArgument))]
        public S1 StructFieldWrapped(ParamWrapper<S1> p1) => p1.Value;
        [Benchmark]
        [ArgumentsSource(nameof(StructPropertyWrappedArgument))]
        public S1 StructPropertyWrapped(ParamWrapperWithValueAsProperty<S1> p1) => p1.Value;

        public class C1 { public int Int1; public int Int2; public int Int3; public override string ToString() => ClassParamDisplayText; }
        public IEnumerable<C1> ClassDirectArgument() { yield return new C1(); }
        public IEnumerable<ParamWrapper<C1>> ClassFieldWrappedArgument() { yield return new ParamWrapper<C1>(new C1(), ClassParamDisplayText); }
        public IEnumerable<ParamWrapperWithValueAsProperty<C1>> ClassPropertyWrappedArgument() { yield return new ParamWrapperWithValueAsProperty<C1>(new C1(), ClassParamDisplayText); }

        [Benchmark(Baseline = true)]
        [ArgumentsSource(nameof(ClassDirectArgument))]
        public C1 ClassDirect(C1 p1) => p1;
        [Benchmark]
        [ArgumentsSource(nameof(ClassFieldWrappedArgument))]
        public C1 ClassFieldWrapped(ParamWrapper<C1> p1) => p1.Value;
        [Benchmark]
        [ArgumentsSource(nameof(ClassPropertyWrappedArgument))]
        public C1 ClassPropertyWrapped(ParamWrapperWithValueAsProperty<C1> p1) => p1.Value;
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
                // except that it also excludes "Metrics" (code size in this case).
                .ReplaceExporters(MarkdownExporter.Console, JsonExporter.FullCompressed)
                .ReplaceLoggers(ConsoleLogger.Unicode)
                // Disassembler runs for every job. Results after DryJob are *NOT* optimized,
                // after Job with multiple runs they eventually are (tiered compilation).
                .AddDiagnoser(new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig()))
                .AddFilter(whatifFilter)
                // Microseconds would be better, but has a few 0 values (Although decimal places of ns
                // probably doesn't have any meaning).
                //.WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Microsecond))
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
