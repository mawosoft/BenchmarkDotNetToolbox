// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat;

internal static class SummaryWrapper
{
    private static readonly ConstructorInfo? s_ctorStable;
    private static readonly ConstructorInfo? s_ctorNightly;

    [SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline",
        Justification = "Multiple interdependent fields.")]
    static SummaryWrapper()
    {
        s_ctorStable = typeof(Summary).GetConstructor(new[]
        {
            typeof(string),
            typeof(ImmutableArray<BenchmarkReport>),
            typeof(HostEnvironmentInfo),
            typeof(string),
            typeof(string),
            typeof(TimeSpan),
            typeof(CultureInfo),
            typeof(ImmutableArray<ValidationError>),
            typeof(ImmutableArray<IColumnHidingRule>)
        });
        if (s_ctorStable is null)
        {
            s_ctorNightly = typeof(Summary).GetConstructor(new[]
            {
                typeof(string),
                typeof(ImmutableArray<BenchmarkReport>),
                typeof(HostEnvironmentInfo),
                typeof(string),
                typeof(string),
                typeof(TimeSpan),
                typeof(CultureInfo),
                typeof(ImmutableArray<ValidationError>),
                typeof(ImmutableArray<IColumnHidingRule>),
                typeof(SummaryStyle)
            });
        }
    }

    public static Summary Create(
        string title,
        ImmutableArray<BenchmarkReport> reports,
        HostEnvironmentInfo hostEnvironmentInfo,
        string resultsDirectoryPath,
        string logFilePath,
        TimeSpan totalTime,
        CultureInfo cultureInfo,
        ImmutableArray<ValidationError> validationErrors,
        ImmutableArray<IColumnHidingRule> columnHidingRules,
        SummaryStyle? summaryStyle = null)
    {
        if (s_ctorStable != null)
        {
            return (Summary)s_ctorStable.Invoke(new object?[]
            {
                title,
                reports,
                hostEnvironmentInfo,
                resultsDirectoryPath,
                logFilePath,
                totalTime,
                cultureInfo,
                validationErrors,
                columnHidingRules
            });
        }
        else if (s_ctorNightly != null)
        {
            return (Summary)s_ctorNightly.Invoke(new object?[]
            {
                title,
                reports,
                hostEnvironmentInfo,
                resultsDirectoryPath,
                logFilePath,
                totalTime,
                cultureInfo,
                validationErrors,
                columnHidingRules,
                summaryStyle
            });
        }
        else
        {
            throw new MissingMethodException(nameof(Summary), ".ctor");
        }
    }
}
