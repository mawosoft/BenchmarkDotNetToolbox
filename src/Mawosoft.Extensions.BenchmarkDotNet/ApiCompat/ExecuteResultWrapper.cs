// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.Results;

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat
{
    internal static class ExecuteResultWrapper
    {
        private static readonly ConstructorInfo? s_ctorNightly;

        static ExecuteResultWrapper()
        {
            s_ctorNightly = typeof(ExecuteResult).GetConstructor(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                new Type[] { typeof(List<Measurement>), typeof(GcStats), typeof(ThreadingStats) },
                null);
        }

        public static ExecuteResult CreateNightly(
            List<Measurement> measurements,
            GcStats gcStats,
            ThreadingStats threadingStats)
        {
            return s_ctorNightly != null
                ? (ExecuteResult)s_ctorNightly.Invoke(new object[] { measurements, gcStats, threadingStats })
                : throw new MissingMethodException(nameof(ExecuteResult), ".ctor");
        }
    }
}
