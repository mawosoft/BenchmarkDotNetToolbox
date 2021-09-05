// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Loggers;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    public static class ManualConfigExtensions
    {
        // Note:
        // The main use case for Replacers is ManualConfig config = ManualConfig.Create(DefaultConfig.Instance);
        // As such, you only need them for Columns, Exporters, and Loggers. There is no reason to replace the
        // default Analysers and Validators, and everything else is empty by default. Plus, BDN will add mandatory
        // Validators anyway when finalizing the Config.

        public static ManualConfig ReplaceLoggers(this ManualConfig config, params ILogger[] newLoggers)
        {
            List<ILogger> loggers = config.GetLoggers() as List<ILogger>
                ?? throw new InvalidOperationException("Could not get loggers as List<>.");
            loggers.Clear();
            return config.AddLogger(newLoggers);
        }

        public static ManualConfig ReplaceExporters(this ManualConfig config, params IExporter[] newExporters)
        {
            List<IExporter> exporters = config.GetExporters() as List<IExporter>
                ?? throw new InvalidOperationException("Could not get exporters as List<>.");
            exporters.Clear();
            return config.AddExporter(newExporters);
        }

        public static ManualConfig ReplaceColumnCategory(this ManualConfig config, params IColumn[] newColumns)
            => config.RemoveColumnsByCategory(newColumns.Select(c => c.Category).Distinct().ToArray())
                     .AddColumn(newColumns);

        public static ManualConfig ReplaceColumnCategory(this ManualConfig config, ColumnCategory columnCategory,
                                                         params IColumnProvider[] newColumnProviders)
            => config.RemoveColumnsByCategory(columnCategory).AddColumnProvider(newColumnProviders);

        public static ManualConfig RemoveColumnsByCategory(this ManualConfig config, params ColumnCategory[] categories)
        {
            List<IColumnProvider> providers = config.GetColumnProviders() as List<IColumnProvider>
                ?? throw new InvalidOperationException("Could not get column providers as List<>.");
            Core(providers, categories);
            return config;

            static void Core(List<IColumnProvider> providers, ColumnCategory[] categories)
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

        private static readonly Type[] s_nonReplacableColumns = { typeof(TargetMethodColumn) };
        private static readonly Lazy<(Type type, ColumnCategory category)[]> s_knownColumnProviders = new(() => new[] {
            (DefaultColumnProviders.Job.GetType(), ColumnCategory.Job),
            (DefaultColumnProviders.Statistics.GetType(), ColumnCategory.Statistics),
            (DefaultColumnProviders.Params.GetType(), ColumnCategory.Params),
            (DefaultColumnProviders.Metrics.GetType(), ColumnCategory.Metric),
            (typeof(RecyclableParamsColumnProvider), ColumnCategory.Params),
        });

        // IConfig overloads for extensions above

        public static ManualConfig ReplaceColumnCategory(this IConfig config, params IColumn[] newColumns)
            => ManualConfig.Create(config).ReplaceColumnCategory(newColumns);

        public static ManualConfig ReplaceColumnCategory(this IConfig config, ColumnCategory columnCategory,
                                                         params IColumnProvider[] newColumnProviders)
            => ManualConfig.Create(config).ReplaceColumnCategory(columnCategory, newColumnProviders);

        public static ManualConfig RemoveColumnsByCategory(IConfig config, params ColumnCategory[] categories)
            => ManualConfig.Create(config).RemoveColumnsByCategory(categories);

        public static ManualConfig ReplaceExporters(this IConfig config, params IExporter[] newExporters)
            => ManualConfig.Create(config).ReplaceExporters(newExporters);

        public static ManualConfig ReplaceLoggers(this IConfig config, params ILogger[] newLoggers)
            => ManualConfig.Create(config).ReplaceLoggers(newLoggers);
    }
}
