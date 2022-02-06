// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

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

namespace Mawosoft.Extensions.BenchmarkDotNet
{
    /// <summary>
    /// A wrapper and extension for <see cref="BenchmarkConverter"/>, collecting the converted benchmarks,
    /// executing them, and optionally overriding any global and local <see cref="Job"/> configurations.
    /// </summary>
    public class BenchmarkRunInfos
    {
        /// <summary>
        /// A predefined Job instance that can be used as override job.
        /// </summary>
        public static readonly Job FastInProcessJob =
            new Job("FastInProc", Job.Dry.WithToolchain(InProcessEmitToolchain.Instance)).Freeze();

        private readonly List<BenchmarkRunInfo> _items = new();

        /// <summary>
        /// Gets or sets the global config to use for subsequent benchmark conversions.
        /// </summary>
        public IConfig? Config { get; set; }

        /// <summary>
        /// Gets or sets the override job to use for subsequent benchmark conversions.
        /// </summary>
        public Job? OverrideJob { get; set; }

        /// <summary>
        /// Gets a read-only collection of the converted benchmarks.
        /// </summary>
        public IReadOnlyList<BenchmarkRunInfo> Items => _items;

        /// <summary>
        /// Gets the number of converted <see cref="BenchmarkRunInfo"/> elements.
        /// </summary>
        public int Count => _items.Count;

        /// <summary>
        /// Gets the converted <see cref="BenchmarkRunInfo"/> element at the specified index.
        /// </summary>
        public BenchmarkRunInfo this[int index] => _items[index];

        /// <summary>
        /// Adds a <see cref="BenchmarkRunInfo"/> element and applies the <see cref="OverrideJob"/>
        /// if one is specified.
        /// </summary>
        public void Add(BenchmarkRunInfo benchmarkRunInfo) => PostProcessConverter(null, benchmarkRunInfo);

        /// <summary>
        /// Adds a collection of <see cref="BenchmarkRunInfo"/> elements and applies the <see cref="OverrideJob"/>
        /// if one is specified.
        /// </summary>
        public void AddRange(IEnumerable<BenchmarkRunInfo> benchmarkRunInfos)
            => PostProcessConverter(null, benchmarkRunInfos.ToArray());

        /// <summary>
        /// Clears the list of <see cref="BenchmarkRunInfo"/> elements and optionally disposes them.
        /// </summary>
        public void Clear(bool dispose)
        {
            if (dispose)
            {
                _items.ForEach(bri => bri.Dispose());
            }
            _items.Clear();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BenchmarkRunInfos"/> class.
        /// </summary>
        public BenchmarkRunInfos() : this(null, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="BenchmarkRunInfos"/> class with an optional global
        /// config and an optional override job.
        /// </summary>
        public BenchmarkRunInfos(IConfig? globalConfig, Job? overrideJob)
        {
            Config = globalConfig;
            OverrideJob = overrideJob;
        }

        /// <summary>
        /// Sets the predefined override job (<see cref="FastInProcessJob"/>) when called from code compiled
        /// with the conditional <c>DEBUG</c> symbol defined.
        /// </summary>
        [Conditional("DEBUG")]
        public void DebugUseDefaultOverrideJob() => OverrideJob = FastInProcessJob;

        /// <summary>
        /// Converts all types with benchmarks in the given assembly and stores the results.
        /// </summary>
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

        /// <summary>
        /// Converts the named benchmark methods of the given type and stores the results.
        /// </summary>
        public void ConvertMethodsToBenchmarks(Type containingType, params string[] benchmarkMethodNames)
            => PostProcessConverter(PreProcessConverter(),
                BenchmarkConverter.MethodsToBenchmarks(containingType,
                    benchmarkMethodNames
                        .Select(
                            bmn => containingType.GetMethod(bmn)
                                   ?? throw new MissingMethodException(containingType.Name, bmn))
                        .ToArray(),
                    Config));

        /// <summary>
        /// Converts the specified benchmark methods of the given type and stores the results.
        /// </summary>
        public void ConvertMethodsToBenchmarks(Type containingType, params MethodInfo[] benchmarkMethods)
            => PostProcessConverter(PreProcessConverter(),
                BenchmarkConverter.MethodsToBenchmarks(containingType, benchmarkMethods, Config));

        /// <summary>
        /// Compiles the given C# source code, converts the contained benchmark methods, and stores the results.
        /// </summary>
        public void ConvertSourceToBenchmarks(string source)
            => PostProcessConverter(PreProcessConverter(), BenchmarkConverter.SourceToBenchmarks(source, Config));

        /// <summary>
        /// Converts all benchmark methods of the given type and stores the results.
        /// </summary>
        public void ConvertTypeToBenchmarks(Type type)
            => PostProcessConverter(PreProcessConverter(), BenchmarkConverter.TypeToBenchmarks(type, Config));

        /// <summary>
        /// Reads and compiles C# source code from the given Url, converts the contained benchmark methods,
        /// and stores the results.
        /// </summary>
        public void ConvertUrlToBenchmarks(string url)
            => PostProcessConverter(PreProcessConverter(), BenchmarkConverter.UrlToBenchmarks(url, Config));

        /// <summary>
        /// Runs all converted benchmarks and returns the results.
        /// </summary>
        public Summary[] RunAll() => BenchmarkRunner.Run(_items.ToArray());

        private WhatifFilter? PreProcessConverter()
        {
            // If an enabled WhatifFilter exists, we want it to filter the results of our own conversion,
            // not the one performed by BenchmarkConverter. Thus, we disable it here and turn it back on
            // in the post processor.
            if (OverrideJob != null
                && Config?.GetFilters().SingleOrDefault(f => f is WhatifFilter) is WhatifFilter filter
                && filter.Enabled)
            {
                filter.Enabled = false;
                return filter;
            }
            return null;
        }

        private void PostProcessConverter(WhatifFilter? whatifFilter, params BenchmarkRunInfo[] runInfos)
        {
            if (whatifFilter != null)
            {
                // Reenable filter after BenchmarkConverter has been called
                whatifFilter.Enabled = true;
            }
            if (OverrideJob == null)
            {
                Debug.Assert(whatifFilter == null);
                _items.AddRange(runInfos.Where(ri => ri.BenchmarksCases.Length > 0));
            }
            else
            {
                // We keep the hashset here in case we want to go back to support multiple override jobs.
                HashSet<Job> jobs = new();
                jobs.Add(OverrideJob);
                foreach (BenchmarkRunInfo runInfo in runInfos)
                {
                    HashSet<BenchmarkCase> oldBenchmarkCases =
                        new(runInfo.BenchmarksCases, BenchmarkCaseWithoutJobEqualityComparer.Instance);
                    List<BenchmarkCase> newBenchmarkCases = new();
                    foreach (BenchmarkCase benchmarkCase in oldBenchmarkCases)
                    {
                        foreach (Job job in jobs)
                        {
                            BenchmarkCase newBenchmarkCase =
                                BenchmarkCase.Create(benchmarkCase.Descriptor, job, benchmarkCase.Parameters,
                                                     benchmarkCase.Config);
                            if (whatifFilter == null || whatifFilter.Predicate(newBenchmarkCase))
                            {
                                newBenchmarkCases.Add(newBenchmarkCase);
                            }
                        }
                    }
                    if (newBenchmarkCases.Count > 0)
                    {
                        _items.Add(new BenchmarkRunInfo(newBenchmarkCases.ToArray(), runInfo.Type, runInfo.Config));
                    }
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
