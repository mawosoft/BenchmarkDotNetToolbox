// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat;

internal static class ExecuteResultWrapper
{
    private static readonly ConstructorInfo? s_ctorInternalStable;

    static ExecuteResultWrapper()
    {
        s_ctorInternalStable = typeof(ExecuteResult).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
            new Type[] { typeof(List<Measurement>), typeof(GcStats), typeof(ThreadingStats), typeof(double) },
            null);
    }

    public static ExecuteResult Create(
        IEnumerable<Measurement> measurements,
        GcStats gcStats,
        ThreadingStats threadingStats,
        double exceptionFrequency)
    {
        if (s_ctorInternalStable is not null)
        {
            return (ExecuteResult)s_ctorInternalStable.Invoke(new object[] { measurements.ToList(), gcStats, threadingStats, exceptionFrequency });
        }
        else
        {
            throw new MissingMethodException(nameof(ExecuteResult), ".ctor");
        }
    }
}
