// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Validators;

namespace Mawosoft.Extensions.BenchmarkDotNet
{
    /// <summary>
    /// <see cref="ManualConfig"/> and <see cref="IConfig"/> extension methods for replacing parts
    /// of an existing config - for example, default columns with custom ones.
    /// </summary>
    public static class ConfigExtensions
    {
        /// <summary>
        /// Replaces existing columns belonging to the same category or categories as <c>newColumns</c>
        /// with the given new ones.
        /// </summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this ManualConfig config, params IColumn[] newColumns)
        {
            ColumnCategoryExtensions.RemoveColumnsByCategory(
                AsList(config.GetColumnProviders()),
                newColumns.Select(c => c.GetExtendedColumnCategory()).Distinct()
                );
            return config.AddColumn(newColumns);
        }

        /// <summary>
        /// Replaces existing columns belonging to the same category or categories as <c>newColumns</c>
        /// with the given new ones.
        /// </summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this IConfig config, params IColumn[] newColumns)
            => ManualConfig.Create(config).ReplaceColumnCategory(newColumns);

        /// <summary>
        /// Replaces existing columns belonging to the same category or categories as <c>newColumnProviders</c>
        /// with the given new ones.
        /// </summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this ManualConfig config,
                                                         params IColumnProvider[] newColumnProviders)
        {
            ColumnCategoryExtensions.RemoveColumnsByCategory(
                AsList(config.GetColumnProviders()),
                newColumnProviders.SelectMany(cp => cp.GetExtendedColumnCategories()).Distinct()
                );
            return config.AddColumnProvider(newColumnProviders);
        }

        /// <summary>
        /// Replaces existing columns belonging to the same category or categories as <c>newColumnProviders</c>
        /// with the given new ones.
        /// </summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this IConfig config,
                                                         params IColumnProvider[] newColumnProviders)
            => ManualConfig.Create(config).ReplaceColumnCategory(newColumnProviders);

        /// <summary>Removes existing columns of the specified category or categories.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig RemoveColumnsByCategory(this ManualConfig config,
                                                           params ColumnCategory[] categories)
        {
            ColumnCategoryExtensions.RemoveColumnsByCategory(
                AsList(config.GetColumnProviders()),
                categories.Select(c => c.ToExtended())
                );
            return config;
        }

        /// <summary>Removes existing columns of the specified category or categories.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig RemoveColumnsByCategory(this IConfig config, params ColumnCategory[] categories)
            => ManualConfig.Create(config).RemoveColumnsByCategory(categories);

        /// <summary>Replaces all exporters with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceExporters(this ManualConfig config, params IExporter[] newExporters)
        {
            ClearEnumerable(config.GetExporters());
            return config.AddExporter(newExporters);
        }

        /// <summary>Replaces all exporters with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceExporters(this IConfig config, params IExporter[] newExporters)
            => ManualConfig.Create(config).ReplaceExporters(newExporters);

        /// <summary>Replaces all loggers with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceLoggers(this ManualConfig config, params ILogger[] newLoggers)
        {
            ClearEnumerable(config.GetLoggers());
            return config.AddLogger(newLoggers);
        }

        /// <summary>Replaces all loggers with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceLoggers(this IConfig config, params ILogger[] newLoggers)
            => ManualConfig.Create(config).ReplaceLoggers(newLoggers);

        /// <summary>Replaces all diagnosers with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceDiagnosers(this ManualConfig config, params IDiagnoser[] newDiagnosers)
        {
            ClearEnumerable(config.GetDiagnosers());
            return config.AddDiagnoser(newDiagnosers);
        }

        /// <summary>Replaces all diagnosers with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceDiagnosers(this IConfig config, params IDiagnoser[] newDiagnosers)
            => ManualConfig.Create(config).ReplaceDiagnosers(newDiagnosers);

        /// <summary>Replaces all analyzers with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceAnalysers(this ManualConfig config, params IAnalyser[] newAnalysers)
        {
            ClearEnumerable(config.GetAnalysers());
            return config.AddAnalyser(newAnalysers);
        }

        /// <summary>Replaces all analysers with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceAnalysers(this IConfig config, params IAnalyser[] newAnalysers)
            => ManualConfig.Create(config).ReplaceAnalysers(newAnalysers);

        /// <summary>Replaces all jobs with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceJobs(this ManualConfig config, params Job[] newJobs)
        {
            ClearEnumerable(config.GetJobs());
            return config.AddJob(newJobs);
        }

        /// <summary>Replaces all jobs with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceJobs(this IConfig config, params Job[] newJobs)
            => ManualConfig.Create(config).ReplaceJobs(newJobs);

        /// <summary>Replaces all validators with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceValidators(this ManualConfig config, params IValidator[] newValidators)
        {
            ClearEnumerable(config.GetValidators());
            return config.AddValidator(newValidators);
        }

        /// <summary>Replaces all validators with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceValidators(this IConfig config, params IValidator[] newValidators)
            => ManualConfig.Create(config).ReplaceValidators(newValidators);

        /// <summary>Replaces all hardware counters with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceHardwareCounters(this ManualConfig config,
                                                           params HardwareCounter[] newHardwareCounters)
        {
            ClearEnumerable(config.GetHardwareCounters());
            return config.AddHardwareCounters(newHardwareCounters);
        }

        /// <summary>Replaces all hardware counters with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceHardwareCounters(this IConfig config,
                                                           params HardwareCounter[] newHardwareCounters)
            => ManualConfig.Create(config).ReplaceHardwareCounters(newHardwareCounters);

        /// <summary>Replaces all filters with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceFilters(this ManualConfig config, params IFilter[] newFilters)
        {
            ClearEnumerable(config.GetFilters());
            return config.AddFilter(newFilters);
        }

        /// <summary>Replaces all filters with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceFilters(this IConfig config, params IFilter[] newFilters)
            => ManualConfig.Create(config).ReplaceFilters(newFilters);

        /// <summary>Replaces all logical group rules with the given new ones.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceLogicalGroupRules(this ManualConfig config,
                                                            params BenchmarkLogicalGroupRule[] newLogicalGroupRules)
        {
            ClearEnumerable(config.GetLogicalGroupRules());
            return config.AddLogicalGroupRules(newLogicalGroupRules);
        }

        /// <summary>Replaces all logical group rules with the given new ones.</summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceLogicalGroupRules(this IConfig config,
                                                            params BenchmarkLogicalGroupRule[] newLogicalGroupRules)
            => ManualConfig.Create(config).ReplaceLogicalGroupRules(newLogicalGroupRules);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static List<T> AsList<T>(IEnumerable<T> enumerable)
            => enumerable as List<T>
            ?? throw new InvalidOperationException($"Failed to get List<{typeof(T).Name}>.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearEnumerable<T>(IEnumerable<T> enumerable)
        {
            if (enumerable is List<T> list)
                list.Clear();
            else if (enumerable is HashSet<T> set)
                set.Clear();
            else
                throw new InvalidOperationException(
                    $"Failed to get List<{typeof(T).Name}> or HashSet<{typeof(T).Name}>.");
        }
    }
}
