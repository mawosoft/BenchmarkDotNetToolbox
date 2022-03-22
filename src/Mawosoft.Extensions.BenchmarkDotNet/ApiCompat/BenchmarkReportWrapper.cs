// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.Results;

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat
{
    internal static class BenchmarkReportWrapper
    {
        private static readonly ConstructorInfo? s_ctorRelease;
        private static readonly ConstructorInfo? s_ctorNightly;

        static BenchmarkReportWrapper()
        {
            // TODO Init via ctor okay? Lazy as ValueTuple?
            s_ctorRelease = typeof(BenchmarkReport).GetConstructor(new Type[] {
                typeof(bool), typeof(BenchmarkCase), typeof(GenerateResult), typeof(BuildResult),
                typeof(IReadOnlyList<ExecuteResult>), typeof(IReadOnlyList<Measurement>),
                typeof(GcStats), typeof(IReadOnlyList<Metric>)
            });
            if (s_ctorRelease == null)
            {
                s_ctorNightly = typeof(BenchmarkReport).GetConstructor(new Type[] {
                    typeof(bool), typeof(BenchmarkCase), typeof(GenerateResult), typeof(BuildResult),
                    typeof(IReadOnlyList<ExecuteResult>), typeof(IReadOnlyList<Metric>)
                });
            }
        }

        public static BenchmarkReport Create(
            bool success,
            BenchmarkCase benchmarkCase,
            GenerateResult generateResult,
            BuildResult buildResult,
            IReadOnlyList<ExecuteResult> executeResults,
            IReadOnlyList<Measurement> allMeasurements,
            GcStats gcStats,
            IReadOnlyList<Metric> metrics)
        {
            if (s_ctorRelease != null)
            {
                return (BenchmarkReport)s_ctorRelease.Invoke(new object[] {
                    success, benchmarkCase, generateResult, buildResult, executeResults,
                    allMeasurements, gcStats, metrics
                });
            }
            else if (s_ctorNightly != null)
            {
                if (executeResults != null && executeResults.Count != 0)
                {
                    throw new ArgumentException(null, nameof(executeResults));
                }
                return (BenchmarkReport)s_ctorNightly.Invoke(new object[] {
                    success, benchmarkCase, generateResult, buildResult,
                    new[] { ExecuteResultWrapper.CreateNightly(allMeasurements.ToList(), gcStats, default) },
                    metrics
                });
            }
            else
            {
                throw new MissingMethodException(nameof(BenchmarkReport), ".ctor");
            }
        }
    }
}
