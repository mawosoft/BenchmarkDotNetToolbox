// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat;

internal static class SummaryWrapper
{
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
        return new(
                title,
                reports,
                hostEnvironmentInfo,
                resultsDirectoryPath,
                logFilePath,
                totalTime,
                cultureInfo,
                validationErrors,
                columnHidingRules,
                summaryStyle!);
    }
}
