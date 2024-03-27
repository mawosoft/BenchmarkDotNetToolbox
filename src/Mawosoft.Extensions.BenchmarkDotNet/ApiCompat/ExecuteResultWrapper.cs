// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat;

internal static class ExecuteResultWrapper
{
    private static readonly ConstructorInfo? s_ctorInternalStable =
        typeof(ExecuteResult).GetConstructor(
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null,
            [
                typeof(List<Measurement>),
                typeof(GcStats),
                typeof(ThreadingStats),
                typeof(double)
            ],
            null);

    public static ExecuteResult Create(
        IEnumerable<Measurement> measurements,
        GcStats gcStats,
        ThreadingStats threadingStats,
        double exceptionFrequency)
    {
        return s_ctorInternalStable is not null
            ? (ExecuteResult)s_ctorInternalStable.Invoke(
                [measurements.ToList(), gcStats, threadingStats, exceptionFrequency])
            : throw new MissingMethodException(nameof(ExecuteResult), ".ctor");
    }
}
