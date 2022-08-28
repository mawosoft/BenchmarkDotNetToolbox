// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.Results;

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat
{
    internal static class ExecuteResultWrapper
    {
        private static readonly ConstructorInfo? s_ctorInternalStable;

        static ExecuteResultWrapper()
        {
            s_ctorInternalStable = typeof(ExecuteResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new Type[] { typeof(List<Measurement>), typeof(GcStats), typeof(ThreadingStats) },
                null);
        }

        public static ExecuteResult Create(
            IEnumerable<Measurement> measurements,
            GcStats gcStats,
            ThreadingStats threadingStats)
        {
            return s_ctorInternalStable != null
                ? (ExecuteResult)s_ctorInternalStable.Invoke(new object[] { measurements.ToList(), gcStats, threadingStats })
                : throw new MissingMethodException(nameof(ExecuteResult), ".ctor");
        }
    }
}
