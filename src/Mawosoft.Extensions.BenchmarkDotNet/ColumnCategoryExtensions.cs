// Copyright (c) 2021-2024 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet;

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
        = new(() => [
            (DefaultColumnProviders.Descriptor.GetType(), ExtendedColumnCategory.TargetMethod),
            (DefaultColumnProviders.Job.GetType(), ExtendedColumnCategory.Job),
            (DefaultColumnProviders.Statistics.GetType(), ExtendedColumnCategory.Statistics),
            (DefaultColumnProviders.Params.GetType(), ExtendedColumnCategory.Params),
            (DefaultColumnProviders.Metrics.GetType(), ExtendedColumnCategory.Metric),
            (typeof(RecyclableParamsColumnProvider), ExtendedColumnCategory.Params),
            (typeof(JobColumnSelectionProvider), ExtendedColumnCategory.Job),
        ]);

    // Get ExtendedColumnCategory(s) either from mapping or by querying for columns
    public static IEnumerable<ExtendedColumnCategory> GetExtendedColumnCategories(this IColumnProvider columnProvider)
    {
        Type providerType = columnProvider.GetType();
        (Type type, ExtendedColumnCategory category) =
            s_knownColumnProviders.Value.FirstOrDefault(item => item.type == providerType);
        if (providerType == type)
        {
            return [category];
        }
        return columnProvider.GetDefaultColumns().Select(c => c.GetExtendedColumnCategory()).Distinct();
    }

    // Needed for mock Summary
    private sealed class MockMetricDescriptor : IMetricDescriptor
    {
        public string Id => nameof(MockMetricDescriptor);
        public string DisplayName => Id;
        public string Legend => string.Empty;
        public string NumberFormat => string.Empty;
        public UnitType UnitType => UnitType.Dimensionless;
        public string Unit => string.Empty;
        public bool TheGreaterTheBetter => false;
        public int PriorityInCategory => 0;
        public bool GetIsAvailable(Metric metric) => true;
    }

    // Mock summary with necessary content to call IColumnProvider.GetColumns(Summary)
    private static readonly Lazy<Summary> s_mockSummary = new(() =>
    {
        BenchmarkCase benchmarkCase = BenchmarkCase.Create(
            new Descriptor(
                type: typeof(ConfigExtensions), workloadMethod: (MethodBase.GetCurrentMethod() as MethodInfo)!,
                baseline: true, categories: ["c1"]
                ),
            Job.Dry,
            new ParameterInstances([ new ParameterInstance(
                new ParameterDefinition("p1", false, [1], true, typeof(int), 0),
                1, SummaryStyle.Default) ]),
            DefaultConfig.Instance.CreateImmutableConfig()
            );
        GenerateResult generateResult = GenerateResultWrapper.Success();
        BuildResult buildResult = BuildResult.Success(generateResult);
        Measurement[] allMeasurements = [
            new Measurement(1, IterationMode.Workload, IterationStage.Result, 1, 1, 1)
        ];
        ExecuteResult[] executeResults = [ExecuteResultWrapper.Create(allMeasurements, default, default, default)];
        Metric[] metrics = [new Metric(new MockMetricDescriptor(), 1)];
        BenchmarkReport benchmarkReport = new(true, benchmarkCase, generateResult, buildResult, executeResults, metrics);
        return SummaryWrapper.Create(
            string.Empty, ImmutableArray.Create(benchmarkReport), HostEnvironmentInfo.GetCurrent(),
            string.Empty, string.Empty, TimeSpan.Zero, SummaryExtensions.GetCultureInfo(null),
            ImmutableArray.Create<ValidationError>(), ImmutableArray.Create<IColumnHidingRule>());
    });

    // Get the default columns from provider
    public static IEnumerable<IColumn> GetDefaultColumns(this IColumnProvider columnProvider)
        => columnProvider.GetColumns(s_mockSummary.Value);

    // Remove columns or entire column providers per given categories
    [SuppressMessage("Maintainability", "CA1508:Avoid dead conditional code", Justification = "False positive due to Debug.Assert().")]
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
                Debug.Assert(columnsField is not null);
                IColumn[]? columns = (columnsField?.GetValue(provider) as IColumn[])?
                    .Where(col => !categories.Contains(col.GetExtendedColumnCategory()))
                    .ToArray();
                if (columns is not null)
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
                Debug.Assert(providersField is not null);
                List<IColumnProvider>? subProviders =
                    (providersField?.GetValue(provider) as IColumnProvider[])?.ToList();
                if (subProviders is not null)
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
