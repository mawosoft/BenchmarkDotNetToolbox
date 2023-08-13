// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet.Tests;

public partial class ConfigExtensionsTests
{
    private static void AssertColumnsEqual(IConfig expected, IConfig actual)
    {
        List<Type> expectedResult = new();
        List<Type> actualResult = new();
        Flatten(expected.GetColumnProviders(), expectedResult);
        Flatten(actual.GetColumnProviders(), actualResult);
        Assert.Equal(expectedResult, actualResult);

        static void Flatten(IEnumerable<IColumnProvider> providers, List<Type> result)
        {
            foreach (IColumnProvider provider in providers)
            {
                result.Add(provider.GetType());
                switch (provider)
                {
                    case CompositeColumnProvider:
                        FieldInfo providersField = typeof(CompositeColumnProvider).GetField("providers",
                            BindingFlags.Instance | BindingFlags.NonPublic)!;
                        Assert.NotNull(providersField);
                        IColumnProvider[] subProviders =
                            (providersField.GetValue(provider) as IColumnProvider[])!;
                        Assert.NotNull(subProviders);
                        Flatten(subProviders, result);
                        break;
                    case SimpleColumnProvider simple:
                        FieldInfo columnsField = typeof(SimpleColumnProvider).GetField("columns",
                            BindingFlags.Instance | BindingFlags.NonPublic)!;
                        Assert.NotNull(columnsField);
                        IColumn[] columns = (columnsField?.GetValue(provider) as IColumn[])!;
                        Assert.NotNull(columns);
                        result.AddRange(columns.Select(c => c.GetType()));
                        break;
                }
            }
        }
    }

    private class ReplaceColumnCategory_WithColumns_TheoryData : TheoryData<IColumnProvider[], IColumn[], IColumnProvider[]>
    {
        public ReplaceColumnCategory_WithColumns_TheoryData()
        {
            Add(new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Job,
                    DefaultColumnProviders.Statistics,
                    DefaultColumnProviders.Params,
                    DefaultColumnProviders.Metrics
                },
                new IColumn[]
                {
                    StatisticColumn.Mean,
                    new CombinedParamsColumn()
                },
                new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Job,
                    DefaultColumnProviders.Metrics
                });
        }
    }

    [Theory]
    [ClassData(typeof(ReplaceColumnCategory_WithColumns_TheoryData))]
    public void ReplaceColumnCategory_WithColumns_Succeeds(IColumnProvider[] input, IColumn[] param, IColumnProvider[] expected)
    {
        IConfig inputIConfig = ManualConfig.CreateEmpty().AddColumnProvider(input);
        ManualConfig inputManualConfig = CloneConfigExcept(inputIConfig, null);
        IConfig expectedIConfig = ManualConfig.CreateEmpty().AddColumnProvider(expected).AddColumn(param);
        IConfig actualIConfig = inputIConfig.ReplaceColumnCategory(param);
        Assert.NotSame(inputIConfig, actualIConfig);
        AssertColumnsEqual(expectedIConfig, actualIConfig);
        ManualConfig actualManualConfig = inputManualConfig.ReplaceColumnCategory(param);
        Assert.Same(inputManualConfig, actualManualConfig);
        AssertColumnsEqual(expectedIConfig, actualManualConfig);
    }

    private class ReplaceColumnCategory_WithProviders_TheoryData : TheoryData<IColumnProvider[], IColumnProvider[], IColumnProvider[]>
    {
        public ReplaceColumnCategory_WithProviders_TheoryData()
        {
            Add(new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Job,
                    DefaultColumnProviders.Statistics,
                    DefaultColumnProviders.Params,
                    DefaultColumnProviders.Metrics
                },
                new IColumnProvider[]
                {
                    new JobColumnSelectionProvider("-all +job"),
                    new RecyclableParamsColumnProvider()
                },
                new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Statistics,
                    DefaultColumnProviders.Metrics
                });
        }
    }

    [Theory]
    [ClassData(typeof(ReplaceColumnCategory_WithProviders_TheoryData))]
    public void ReplaceColumnCategory_WithProviders_Succeeds(IColumnProvider[] input, IColumnProvider[] param, IColumnProvider[] expected)
    {
        IConfig inputIConfig = ManualConfig.CreateEmpty().AddColumnProvider(input);
        ManualConfig inputManualConfig = CloneConfigExcept(inputIConfig, null);
        IConfig expectedIConfig = ManualConfig.CreateEmpty().AddColumnProvider(expected).AddColumnProvider(param);
        IConfig actualIConfig = inputIConfig.ReplaceColumnCategory(param);
        Assert.NotSame(inputIConfig, actualIConfig);
        AssertColumnsEqual(expectedIConfig, actualIConfig);
        ManualConfig actualManualConfig = inputManualConfig.ReplaceColumnCategory(param);
        Assert.Same(inputManualConfig, actualManualConfig);
        AssertColumnsEqual(expectedIConfig, actualManualConfig);
    }

    private class RemoveColumnsByCategory_TheoryData : TheoryData<IColumnProvider[], ColumnCategory[], IColumnProvider[]>
    {
        public RemoveColumnsByCategory_TheoryData()
        {
            Add(new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Job,
                    DefaultColumnProviders.Statistics,
                    DefaultColumnProviders.Params,
                    DefaultColumnProviders.Metrics
                },
                new ColumnCategory[] { ColumnCategory.Job, ColumnCategory.Statistics },
                new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Params,
                    DefaultColumnProviders.Metrics
                });
            Add(new IColumnProvider[]
                {
                    new CompositeColumnProvider
                    (
                        DefaultColumnProviders.Descriptor,
                        DefaultColumnProviders.Job,
                        new SimpleColumnProvider
                        (
                            StatisticColumn.Max,
                            CategoriesColumn.Default,
                            new ParamColumn("param")
                        ),
                        DefaultColumnProviders.Statistics,
                        DefaultColumnProviders.Params,
                        DefaultColumnProviders.Metrics
                    )
                },
                new ColumnCategory[] { ColumnCategory.Statistics, ColumnCategory.Params},
                new IColumnProvider[]
                {
                    new CompositeColumnProvider
                    (
                        DefaultColumnProviders.Descriptor,
                        DefaultColumnProviders.Job,
                        new SimpleColumnProvider
                        (
                            CategoriesColumn.Default
                        ),
                        DefaultColumnProviders.Metrics
                    )
                });
            Add(new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Job,
                    new CompositeColumnProvider
                    (
                        DefaultColumnProviders.Statistics,
                        DefaultColumnProviders.Params
                    ),
                    new SimpleColumnProvider
                    (
                        StatisticColumn.Max,
                        new ParamColumn("param")
                    ),
                    DefaultColumnProviders.Metrics
                },
                new ColumnCategory[] { ColumnCategory.Statistics, ColumnCategory.Params },
                new IColumnProvider[]
                {
                    DefaultColumnProviders.Descriptor,
                    DefaultColumnProviders.Job,
                    DefaultColumnProviders.Metrics
                });
        }
    }

    [Theory]
    [ClassData(typeof(RemoveColumnsByCategory_TheoryData))]
    public void RemoveColumnsByCategory_Succeeds(IColumnProvider[] input, ColumnCategory[] param, IColumnProvider[] expected)
    {
        IConfig inputIConfig = ManualConfig.CreateEmpty().AddColumnProvider(input);
        ManualConfig inputManualConfig = CloneConfigExcept(inputIConfig, null);
        IConfig expectedIConfig = ManualConfig.CreateEmpty().AddColumnProvider(expected);
        IConfig actualIConfig = inputIConfig.RemoveColumnsByCategory(param);
        Assert.NotSame(inputIConfig, actualIConfig);
        AssertColumnsEqual(expectedIConfig, actualIConfig);
        ManualConfig actualManualConfig = inputManualConfig.RemoveColumnsByCategory(param);
        Assert.Same(inputManualConfig, actualManualConfig);
        AssertColumnsEqual(expectedIConfig, actualManualConfig);
    }
}
