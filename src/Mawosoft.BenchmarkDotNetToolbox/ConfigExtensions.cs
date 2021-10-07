// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
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

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>
    /// Extension methods for <see cref="ManualConfig"/> and <see cref="IConfig"/> in general.
    /// </summary>
    public static class ConfigExtensions
    {
        /// <summary>
        /// Replaces existing columns belonging to the same category or categories as <b>newColumns</b>
        /// with the given new ones.
        /// </summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this ManualConfig config, params IColumn[] newColumns)
            => config.RemoveColumnsByCategory(newColumns.Select(c => c.Category).Distinct().ToArray())
                     .AddColumn(newColumns);

        /// <summary>
        /// Replaces existing columns belonging to the same category or categories as <b>newColumns</b>
        /// with the given new ones.
        /// </summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this IConfig config, params IColumn[] newColumns)
            => ManualConfig.Create(config).ReplaceColumnCategory(newColumns);

        /// <summary>
        /// Replaces existing columns of the specified category with the given new ColumnProviders.
        /// </summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this ManualConfig config, ColumnCategory columnCategory,
                                                         params IColumnProvider[] newColumnProviders)
            => config.RemoveColumnsByCategory(columnCategory).AddColumnProvider(newColumnProviders);

        /// <summary>
        /// Replaces existing columns of the specified category with the given new ColumnProviders.
        /// </summary>
        /// <returns>A new instance of <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig ReplaceColumnCategory(this IConfig config, ColumnCategory columnCategory,
                                                         params IColumnProvider[] newColumnProviders)
            => ManualConfig.Create(config).ReplaceColumnCategory(columnCategory, newColumnProviders);

        /// <summary>Removes existing columns of the specified category or categories.</summary>
        /// <returns>The existing <see cref="ManualConfig"/> with changes applied.</returns>
        public static ManualConfig RemoveColumnsByCategory(this ManualConfig config, params ColumnCategory[] categories)
        {
            List<IColumnProvider> providers = AsList(config.GetColumnProviders());
            RemoveColumnsByCategoryCore(providers, categories);
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
            ClearList(config.GetExporters());
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
            ClearList(config.GetLoggers());
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
            ClearList(config.GetDiagnosers());
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
            ClearList(config.GetAnalysers());
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
            ClearList(config.GetJobs());
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
            ClearList(config.GetValidators());
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
            ClearHashSet(config.GetHardwareCounters());
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
            ClearList(config.GetFilters());
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
            ClearHashSet(config.GetLogicalGroupRules());
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
        private static HashSet<T> AsHashSet<T>(IEnumerable<T> enumerable)
            => enumerable as HashSet<T>
            ?? throw new InvalidOperationException($"Failed to get HashSet<{typeof(T).Name}>.");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearList<T>(IEnumerable<T> enumerable) => AsList(enumerable).Clear();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void ClearHashSet<T>(IEnumerable<T> enumerable) => AsHashSet(enumerable).Clear();


        private static readonly Type[] s_nonReplacableColumns = {
            // TargetMethodColumn belongs to ColumnCategory.Job, but should not be replaced.
            typeof(TargetMethodColumn)
        };

        // Maps known ColumnProviders (which don't report a ColumnCategory) to ColumnCategory
        private static readonly Lazy<(Type type, ColumnCategory category)[]> s_knownColumnProviders = new(() => new[] {
            (DefaultColumnProviders.Job.GetType(), ColumnCategory.Job),
            (DefaultColumnProviders.Statistics.GetType(), ColumnCategory.Statistics),
            (DefaultColumnProviders.Params.GetType(), ColumnCategory.Params),
            (DefaultColumnProviders.Metrics.GetType(), ColumnCategory.Metric),
            (typeof(RecyclableParamsColumnProvider), ColumnCategory.Params),
            (typeof(JobColumnSelectionProvider), ColumnCategory.Job),
        });

        private static void RemoveColumnsByCategoryCore(List<IColumnProvider> providers, ColumnCategory[] categories)
        {
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                bool remove = false;
                IColumnProvider provider = providers[i];
                Type providerType = provider.GetType();
                (Type type, ColumnCategory category) =
                    s_knownColumnProviders.Value.FirstOrDefault(item => item.type == providerType);
                if (providerType == type)
                {
                    if (categories.Contains(category))
                    {
                        remove = true;
                    }
                }
                else if (providerType == typeof(SimpleColumnProvider))
                {
                    FieldInfo? columnsField = typeof(SimpleColumnProvider).GetField("columns",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Debug.Assert(columnsField != null);
                    IColumn[]? columns = (columnsField?.GetValue(provider) as IColumn[])?
                        .Where(col => s_nonReplacableColumns.Contains(col.GetType())
                               || !categories.Contains(col.Category))
                        .ToArray();
                    if (columns != null)
                    {
                        if (columns.Length == 0)
                        {
                            remove = true;
                        }
                        else
                        {
                            columnsField!.SetValue(provider, columns);
                        }
                    }

                }
                else if (providerType == typeof(CompositeColumnProvider))
                {
                    FieldInfo? providersField = typeof(CompositeColumnProvider).GetField("providers",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Debug.Assert(providersField != null);
                    List<IColumnProvider>? subProviders =
                        (providersField?.GetValue(provider) as IColumnProvider[])?.ToList();
                    if (subProviders != null)
                    {
                        RemoveColumnsByCategoryCore(subProviders, categories);
                        if (subProviders.Count == 0)
                        {
                            remove = true;
                        }
                        else
                        {
                            providersField!.SetValue(provider, subProviders.ToArray());
                        }
                    }
                }
                if (remove)
                {
                    providers.RemoveAt(i);
                }
            }
        }
    }
}
