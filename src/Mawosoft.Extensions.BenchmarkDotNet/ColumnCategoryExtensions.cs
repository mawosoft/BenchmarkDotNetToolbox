// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;
using BenchmarkDotNet.Validators;

namespace Mawosoft.Extensions.BenchmarkDotNet
{
    internal static class ColumnCategoryExtensions
    {
        // Adds "Unknown" and splits off some columns that report ColumnCategory.Job
        public enum ExtendedColumnCategory
        {
            Unknown, TargetMethod, Category, Job, Params, Statistics, Baseline, Custom, Meta, Metric
        }

        public static ExtendedColumnCategory ToExtended(this ColumnCategory category)
            => category switch
            {
                ColumnCategory.Job => ExtendedColumnCategory.Job,
                ColumnCategory.Params => ExtendedColumnCategory.Params,
                ColumnCategory.Statistics => ExtendedColumnCategory.Statistics,
                ColumnCategory.Baseline => ExtendedColumnCategory.Baseline,
                ColumnCategory.Custom => ExtendedColumnCategory.Custom,
                ColumnCategory.Meta => ExtendedColumnCategory.Meta,
                ColumnCategory.Metric => ExtendedColumnCategory.Metric,
                _ => ExtendedColumnCategory.Unknown
            };

        // Map some ColumnCategory.Job columns to different ExtendedColumnCategory
        public static ExtendedColumnCategory GetExtendedColumnCategory(this IColumn column)
            => column switch
            {
                // Both of these are ColumnCategory.Job
                TargetMethodColumn => ExtendedColumnCategory.TargetMethod,
                CategoriesColumn => ExtendedColumnCategory.Category,
                _ => column.Category.ToExtended()
            };

        // Map known ColumnProviders to ExtendedColumnCategory
        private static readonly Lazy<(Type type, ExtendedColumnCategory category)[]> s_knownColumnProviders
            = new(() => new[] {
                (DefaultColumnProviders.Descriptor.GetType(), ExtendedColumnCategory.TargetMethod),
                (DefaultColumnProviders.Job.GetType(), ExtendedColumnCategory.Job),
                (DefaultColumnProviders.Statistics.GetType(), ExtendedColumnCategory.Statistics),
                (DefaultColumnProviders.Params.GetType(), ExtendedColumnCategory.Params),
                (DefaultColumnProviders.Metrics.GetType(), ExtendedColumnCategory.Metric),
                (typeof(RecyclableParamsColumnProvider), ExtendedColumnCategory.Params),
                (typeof(JobColumnSelectionProvider), ExtendedColumnCategory.Job),
            });

        // Get ExtendedColumnCategory(s) either from mapping or by querying for columns
        public static IEnumerable<ExtendedColumnCategory> GetExtendedColumnCategories(this IColumnProvider columnProvider)
        {
            Type providerType = columnProvider.GetType();
            (Type type, ExtendedColumnCategory category) =
                s_knownColumnProviders.Value.FirstOrDefault(item => item.type == providerType);
            if (providerType == type)
            {
                return new[] { category };
            }
            return columnProvider.GetDefaultColumns().Select(c => c.GetExtendedColumnCategory()).Distinct();
        }

        // Needed for mock Summary
        private class MockMetricDescriptor : IMetricDescriptor
        {
            public string Id => nameof(MockMetricDescriptor);
            public string DisplayName => Id;
            public string Legend => string.Empty;
            public string NumberFormat => string.Empty;
            public UnitType UnitType => UnitType.Dimensionless;
            public string Unit => string.Empty;
            public bool TheGreaterTheBetter => false;
            public int PriorityInCategory => 0;
        }

        // Mock summary with necessary content to call IColumnProvider.GetColumns(Summary)
        private static readonly Lazy<Summary> s_mockSummary = new(() =>
        {
            BenchmarkCase benchmarkCase = BenchmarkCase.Create(
                new Descriptor(
                    type: typeof(ConfigExtensions), workloadMethod: MethodBase.GetCurrentMethod() as MethodInfo,
                    baseline: true, categories: new[] { "c1" }
                    ),
                Job.Dry,
                new ParameterInstances(new[] { new ParameterInstance(
                    new ParameterDefinition("p1", false, new object[] { 1 }, true, typeof(int), 0),
                    1, SummaryStyle.Default) }),
                DefaultConfig.Instance.CreateImmutableConfig()
                );
            GenerateResult generateResult = GenerateResult.Success(ArtifactsPaths.Empty, Array.Empty<string>());
            BuildResult buildResult = BuildResult.Success(generateResult);
            BenchmarkReport benchmarkReport = new(
                true, benchmarkCase,
                generateResult,
                buildResult,
                Array.Empty<ExecuteResult>(),
                new[] { new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, 1) },
                default,
                new[] { new Metric(new MockMetricDescriptor(), 1) }
                );
            return new Summary(
                string.Empty, ImmutableArray.Create(benchmarkReport), HostEnvironmentInfo.GetCurrent(),
                string.Empty, string.Empty, TimeSpan.Zero, SummaryExtensions.GetCultureInfo(null),
                ImmutableArray.Create<ValidationError>()
                );
        });

        // Get the default columns from provider
        public static IEnumerable<IColumn> GetDefaultColumns(this IColumnProvider columnProvider)
            => columnProvider.GetColumns(s_mockSummary.Value);

        // Remove columns or entire column providers per given categories
        public static void RemoveColumnsByCategory(List<IColumnProvider> providers,
                                                   IEnumerable<ExtendedColumnCategory> categories)
        {
            for (int i = providers.Count - 1; i >= 0; i--)
            {
                bool remove = false;
                IColumnProvider provider = providers[i];
                Type providerType = provider.GetType();
                if (providerType == typeof(SimpleColumnProvider))
                {
                    FieldInfo? columnsField = typeof(SimpleColumnProvider).GetField("columns",
                        BindingFlags.Instance | BindingFlags.NonPublic);
                    Debug.Assert(columnsField != null);
                    IColumn[]? columns = (columnsField?.GetValue(provider) as IColumn[])?
                        .Where(col => !categories.Contains(col.GetExtendedColumnCategory()))
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
                        RemoveColumnsByCategory(subProviders, categories);
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
                else
                {
                    // If no provider categories remain after removing the given categories,
                    // provider can be removed.
                    if (!provider.GetExtendedColumnCategories().Except(categories).Any())
                    {
                        remove = true;
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
