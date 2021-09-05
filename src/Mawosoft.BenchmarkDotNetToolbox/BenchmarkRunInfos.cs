// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    public class BenchmarkRunInfos
    {
        // Note:
        // Beside providing a nice wrapper + extension for BenchmarkConverter and BenchmarkRunner, the main
        // use case for BenchmarkRunInfos is benchmark debugging.
        // By specifying a replacement job, you can disable all global *and* local job configurations, mutators,
        // etc. without having to temporarly modifying their respective coding and only run a single fast
        // in-process job targeted at the methods you want to debug.
        //
        // Example:
        //
        //     ... build your regular global config ...
        //     if (Debugger.IsAttached && args.Length == 0)
        //     {
        //         config.WithOption(ConfigOptions.DisableOptimizationsValidator, true);
        //         BenchmarkRunInfos runInfos = new(config, BenchmarkRunInfos.FastInProcessJob);
        //         runInfos.ConvertMethodsToBenchmarks(typeof(MyClass1), "Method1");
        //         runInfos.ConvertMethodsToBenchmarks(typeof(MyClass2), "Method2", "Method3");
        //         Summary[] summaries = runInfos.RunAll();
        //     }
        //     else
        //     {
        //         Summary[] summaries = BenchmarkRunner.Run(typeof(Program).Assembly, config, args);
        //     }

        public static Job FastInProcessJob = new("FastInProc", Job.Dry.WithToolchain(InProcessEmitToolchain.Instance));
        public IConfig? Config { get; set; }
        public List<Job> ReplacementJobs { get; }
        public List<BenchmarkRunInfo> Items { get; } = new();
        public int Count => Items.Count;
        public BenchmarkRunInfo this[int index] => Items[index];

        public BenchmarkRunInfos() : this(null) { }
        public BenchmarkRunInfos(IConfig? globalConfig, params Job[] replacementJobs)
        {
            Config = globalConfig;
            ReplacementJobs = replacementJobs?.Length > 0 ? new(replacementJobs) : new();
        }

        [Conditional("DEBUG")]
        public void DebugAddDefaultReplacementJob() => ReplacementJobs.Add(FastInProcessJob);

        public void ConvertAssemblyToBenchmarks(Assembly assembly)
        {
            IEnumerable<Type> types = assembly.GetTypes()
                .Where(t => !t.IsAbstract
                            && !t.IsSealed
                            && !t.IsNotPublic
                            && t.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static)
                                .Any(m => m.GetCustomAttribute<BenchmarkAttribute>(true) != null));
            foreach (Type type in types)
            {
                ConvertTypeToBenchmarks(type);
            }
        }

        public void ConvertMethodsToBenchmarks(Type containingType, params string[] benchmarkMethodNames)
            => PostProcessRunInfos(BenchmarkConverter.MethodsToBenchmarks(containingType,
                   benchmarkMethodNames
                       .Select(bmn => containingType.GetMethod(bmn)
                                   ?? throw new MissingMethodException(containingType.Name, bmn))
                       .ToArray(),
                   Config));

        public void ConvertMethodsToBenchmarks(Type containingType, params MethodInfo[] benchmarkMethods)
            => PostProcessRunInfos(BenchmarkConverter.MethodsToBenchmarks(containingType, benchmarkMethods, Config));
        public void ConvertSourceToBenchmarks(string source)
            => PostProcessRunInfos(BenchmarkConverter.SourceToBenchmarks(source, Config));
        public void ConvertTypeToBenchmarks(Type type)
            => PostProcessRunInfos(BenchmarkConverter.TypeToBenchmarks(type, Config));
        public void ConvertUrlToBenchmarks(string url)
            => PostProcessRunInfos(BenchmarkConverter.UrlToBenchmarks(url, Config));

        public Summary[] RunAll() => BenchmarkRunner.Run(Items.ToArray());

        private void PostProcessRunInfos(params BenchmarkRunInfo[] runInfos)
        {
            if (ReplacementJobs.Count == 0)
            {
                Items.AddRange(runInfos.Where(ri => ri.BenchmarksCases.Length > 0));
                return;
            }
            // TODO Verify that the underlying Dispose implementation is safe for our purposes.
            // IDisposable chain goes: BenchmarkRunInfo -> BenchmarkCase(s) -> ParameterInstances ->
            //                         ParameterInstance(s) -> (Value as IDisposable)?
            // Preliminary tests show:
            // - During conversion, parameters are constructed per method and each job works with
            //   the same cached parameter(s).
            // - Disposal only occurs explicitly after running all benchmarks.
            // => That means it is safe to just discard the unused original benchmark cases.
            foreach (BenchmarkRunInfo runInfo in runInfos)
            {
                HashSet<BenchmarkCase> oldBenchmarkCases =
                    new(runInfo.BenchmarksCases, BenchmarkCaseWithoutJobEqualityComparer.Instance);
                List<BenchmarkCase> newBenchmarkCases = new();
                HashSet<Job> jobs = new(ReplacementJobs);
                foreach (BenchmarkCase benchmarkCase in oldBenchmarkCases)
                {
                    foreach (Job job in jobs)
                    {
                        newBenchmarkCases.Add(BenchmarkCase.Create(benchmarkCase.Descriptor, job,
                            benchmarkCase.Parameters, benchmarkCase.Config));
                    }
                }
                if (newBenchmarkCases.Count > 0)
                {
                    Items.Add(new BenchmarkRunInfo(newBenchmarkCases.ToArray(), runInfo.Type, runInfo.Config));
                }
            }
        }

        private class BenchmarkCaseWithoutJobEqualityComparer : IEqualityComparer<BenchmarkCase>
        {
            public static BenchmarkCaseWithoutJobEqualityComparer Instance = new();
            public bool Equals(BenchmarkCase x, BenchmarkCase y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (x == null || y == null)
                    return false;
                // Folder/DisplayInfo and HasParameters/Arguments are calculated properties
                return ReferenceEquals(x.Descriptor, y.Descriptor)
                    && ReferenceEquals(x.Parameters, y.Parameters)
                    && ReferenceEquals(x.Config, y.Config);
            }

            // netstandard2.0 compat. Otherwise we would use HashCode.Combine().
            public int GetHashCode(BenchmarkCase obj)
                => (obj?.Descriptor?.GetHashCode() ?? 0)
                   ^ (obj?.Parameters?.GetHashCode() ?? 0)
                   ^ (obj?.Config?.GetHashCode() ?? 0);
        }
    }
}
