// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    public static class ManualConfigExtensions
    {
        // Note:
        // The main use case for Replacers is ManualConfig config = ManualConfig.Create(DefaultConfig.Instance);
        // As such, you only need them for Columns, Exporters, and Loggers. There is no reason to replace the
        // default Analysers and Validators, and everything else is empty by default. Plus, BDN will add mandatory
        // Validators anyway when finalizing the Config.

        public static ManualConfig ReplaceColumnCategory(this ManualConfig config, params IColumn[] newColumns)
        {
            var categories = newColumns.GroupBy(c => c.Category).Select(g => g.Key).ToArray();
            RemoveColumnsByCategory(config, categories);
            return config.AddColumn(newColumns);
        }

        public static ManualConfig ReplaceColumnCategory(this ManualConfig config, ColumnCategory columnCategory, params IColumnProvider[] newColumnProviders)
        {
            RemoveColumnsByCategory(config, columnCategory);
            return config.AddColumnProvider(newColumnProviders);
        }

        private static readonly Type[] s_nonReplacableColumns = { typeof(TargetMethodColumn) };
        private static readonly Lazy<(Type type, ColumnCategory category)[]> s_knownColumnProviders = new(() => new[] {
            (DefaultColumnProviders.Job.GetType(), ColumnCategory.Job),
            (DefaultColumnProviders.Statistics.GetType(), ColumnCategory.Statistics),
            (DefaultColumnProviders.Params.GetType(), ColumnCategory.Params),
            (DefaultColumnProviders.Metrics.GetType(), ColumnCategory.Metric),
            (typeof(RecyclableParamsColumnProvider), ColumnCategory.Params),
        });

        private static void RemoveColumnsByCategory(ManualConfig config, params ColumnCategory[] categories)
        {
            List<IColumnProvider> configColumnProviders = config.GetColumnProviders() as List<IColumnProvider> ?? throw new InvalidOperationException("Could not get column providers as List<>.");
            List<IColumnProvider> providers = new(configColumnProviders);
            Core(providers, categories);
            configColumnProviders.Clear();
            configColumnProviders.AddRange(providers);

            static void Core(List<IColumnProvider> providers, ColumnCategory[] categories)
            {
                for (int i = providers.Count - 1; i >= 0; i--)
                {
                    bool remove = false;
                    IColumnProvider provider = providers[i];
                    Type providerType = provider.GetType();
                    (Type type, ColumnCategory category) = s_knownColumnProviders.Value.FirstOrDefault(item => item.type == providerType);
                    if (providerType == type)
                    {
                        if (categories.Contains(category))
                        {
                            remove = true;
                        }
                    }
                    else if (providerType == typeof(SimpleColumnProvider))
                    {
                        FieldInfo? columnsField = typeof(SimpleColumnProvider).GetField("columns", BindingFlags.Instance | BindingFlags.NonPublic);
                        Debug.Assert(columnsField != null);
                        IColumn[]? columns = (columnsField?.GetValue(provider) as IColumn[])?.Where(col => s_nonReplacableColumns.Contains(col.GetType()) || !categories.Contains(col.Category)).ToArray();
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
                        FieldInfo? providersField = typeof(CompositeColumnProvider).GetField("providers", BindingFlags.Instance | BindingFlags.NonPublic);
                        Debug.Assert(providersField != null);
                        List<IColumnProvider>? subProviders = (providersField?.GetValue(provider) as IColumnProvider[])?.ToList();
                        if (subProviders != null)
                        {
                            Core(subProviders, categories);
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

        public static ManualConfig ReplaceExporters(this ManualConfig config, params IExporter[] newExporters)
        {
            List<IExporter> exporters = config.GetExporters() as List<IExporter> ?? throw new InvalidOperationException("Could not get exporters as List<>.");
            exporters.Clear();
            return config.AddExporter(newExporters);
        }

        public static ManualConfig ReplaceLoggers(this ManualConfig config, params ILogger[] newLoggers)
        {
            List<ILogger> loggers = config.GetLoggers() as List<ILogger> ?? throw new InvalidOperationException("Could not get loggers as List<>.");
            loggers.Clear();
            return config.AddLogger(newLoggers);
        }

        public static ManualConfig AddExclusiveJob(this ManualConfig config, params Job[] exclusiveJobs)
        {
            if (config.GetFilters().Any(f => f.GetType() == typeof(ExclusiveJobFilter)))
                throw new InvalidOperationException("Exclusive jobs can only be added once.");
            return config.AddFilter(new ExclusiveJobFilter(exclusiveJobs)).AddJob(exclusiveJobs);
        }

        [Conditional("DEBUG")]
        public static void DebugAddExclusiveJob(this ManualConfig config, params Job[] exclusiveJobs)
        {
            config.AddExclusiveJob(exclusiveJobs);
        }

        public static ManualConfig AddExclusiveDebugJob(this ManualConfig config)
        {
            return config.AddExclusiveJob(new Job("DebugJob", Job.Dry.WithToolchain(InProcessEmitToolchain.Instance)));
        }

        [Conditional("DEBUG")]
        public static void DebugAddExclusiveDebugJob(this ManualConfig config)
        {
            config.AddExclusiveDebugJob();
        }
        private class ExclusiveJobFilter : IFilter
        {
            private readonly Job[] _exclusiveJobs;
            public ExclusiveJobFilter(params Job[] exclusiveJobs) => _exclusiveJobs = exclusiveJobs;
            public bool Predicate(BenchmarkCase benchmarkCase) => _exclusiveJobs.Any(j => ReferenceEquals(j, benchmarkCase.Job));
        }
    }
}
