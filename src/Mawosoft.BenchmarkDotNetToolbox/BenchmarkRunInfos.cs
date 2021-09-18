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
    /// <summary>
    /// A wrapper and extension for <see cref="BenchmarkConverter"/>, maintaining the involved input and output items,
    /// capable of overriding any global or local Job configs for Debug and similar purposes.
    /// </summary>
    /// <remarks>
    /// By specifying a replacement job, you can disable all global *and* local job configurations, mutators,
    /// etc. without having to temporarly modifying their respective coding and only run a single fast
    /// in-process job targeted at the methods you want to debug.
    /// </remarks>
    public class BenchmarkRunInfos
    {
        /// <summary>Predefined Job instance that can be used as replacement job.</summary>
        public static readonly Job FastInProcessJob =
            new Job("FastInProc", Job.Dry.WithToolchain(InProcessEmitToolchain.Instance)).Freeze();
        /// <summary>Global config to use for subsequent ConvertXxx method calls.</summary>
        public IConfig? Config { get; set; }
        /// <summary>List of optional replacement jobs to use for subsequent ConvertXxx method calls.</summary>
        /// <remarks>Use the Add() etc. methods of the <see cref="List{T}>"/> class.</remarks>
        public List<Job> ReplacementJobs { get; }
        /// <summary>List of <see cref="BenchmarkRunInfo"/> items created by ConvertXxx method calls.</summary>
        public List<BenchmarkRunInfo> Items { get; } = new();
        /// <summary>Number of <see cref="BenchmarkRunInfo"/> items created by ConvertXxx method calls.</summary>
        public int Count => Items.Count;
        /// <summary>Get the <see cref="BenchmarkRunInfo"/> item at the specified index.</summary>
        public BenchmarkRunInfo this[int index] => Items[index];

        /// <summary>Initializes a new instance of the <see cref="BenchmarkRunInfos"/> class.</summary>
        public BenchmarkRunInfos() : this(null) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="BenchmarkRunInfos"/> class with an optional global
        /// config and optional replacement jobs.
        /// </summary>
        public BenchmarkRunInfos(IConfig? globalConfig, params Job[] replacementJobs)
        {
            Config = globalConfig;
            ReplacementJobs = replacementJobs?.Length > 0 ? new(replacementJobs) : new();
        }

        /// <summary>
        /// Adds a predefined replacement job when called from code compiled with the conditional
        /// <b>DEBUG</b> symbol defined.
        /// </summary>
        [Conditional("DEBUG")]
        public void DebugAddDefaultReplacementJob() => ReplacementJobs.Add(FastInProcessJob);

        /// <summary>Converts all types with benchmarks in the given assembly.</summary>
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

        /// <summary>Converts the named benchmark methods of the given type.</summary>
        public void ConvertMethodsToBenchmarks(Type containingType, params string[] benchmarkMethodNames)
            => PostProcessRunInfos(BenchmarkConverter.MethodsToBenchmarks(containingType,
                   benchmarkMethodNames
                       .Select(bmn => containingType.GetMethod(bmn)
                                   ?? throw new MissingMethodException(containingType.Name, bmn))
                       .ToArray(),
                   Config));

        /// <summary>Wrapper for <see cref="BenchmarkConverter.MethodsToBenchmarks"/></summary>
        public void ConvertMethodsToBenchmarks(Type containingType, params MethodInfo[] benchmarkMethods)
            => PostProcessRunInfos(BenchmarkConverter.MethodsToBenchmarks(containingType, benchmarkMethods, Config));

        /// <summary>Wrapper for <see cref="BenchmarkConverter.SourceToBenchmarks"/></summary>
        public void ConvertSourceToBenchmarks(string source)
            => PostProcessRunInfos(BenchmarkConverter.SourceToBenchmarks(source, Config));

        /// <summary>Wrapper for <see cref="BenchmarkConverter.TypeToBenchmarks"/></summary>
        public void ConvertTypeToBenchmarks(Type type)
            => PostProcessRunInfos(BenchmarkConverter.TypeToBenchmarks(type, Config));

        /// <summary>Wrapper for <see cref="BenchmarkConverter.UrlToBenchmarks"/></summary>
        public void ConvertUrlToBenchmarks(string url)
            => PostProcessRunInfos(BenchmarkConverter.UrlToBenchmarks(url, Config));

        /// <summary>Runs all converted benchmarks.</summary>
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
            public static readonly BenchmarkCaseWithoutJobEqualityComparer Instance = new();
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
