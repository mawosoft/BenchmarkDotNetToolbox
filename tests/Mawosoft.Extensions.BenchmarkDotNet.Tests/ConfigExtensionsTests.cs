// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet.Tests;

public partial class ConfigExtensionsTests
{
    private static readonly Lazy<List<(Type item, MethodInfo add, MethodInfo get)>> s_methodPairs = new(() =>
    {
        IEnumerable<MethodInfo> getter = typeof(IConfig)
            .GetMethods()
            .Where(m => !m.IsSpecialName
                        && m.ReturnType.IsGenericType
                        && typeof(IEnumerable).IsAssignableFrom(m.ReturnType));
        AssemblyInformationalVersionAttribute v = typeof(BenchmarkCase).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!;
        Assert.NotNull(v);
        if (new NuGetVersion(v.InformationalVersion) < new NuGetVersion("0.13.9-nightly.20230921.71"))
        {
            Assert.Equal(11, getter.Count());
        }
        else
        {
            // EventProcessor, GetEventProcessors(), AddEventProcessor() added in 0.13.9-nightly.20230921.71.
            // Properly working in 0.13.9-nightly.20230922.72
            Assert.Equal(12, getter.Count());
        }
        (MethodInfo m, Type? t)[] adder = typeof(ManualConfig)
            .GetMethods()
            .Where(m => !m.IsSpecialName
                        && m.ReturnType == typeof(ManualConfig)
                        && ((m.Name.StartsWith("Add", StringComparison.Ordinal) && m.Name != "AddColumn")
                            || m.Name == "HideColumns"))
            .Select(m =>
            {
                Type? retType = null;
                ParameterInfo[] pi = m.GetParameters();
                if (pi.Length == 1 && pi[0].ParameterType.IsArray)
                {
                    retType = pi[0].ParameterType.GetElementType();
                }
                return (m, retType);
            }).ToArray();
        return getter.Select(g =>
        {
            Type t = g.ReturnType.GetGenericArguments().Single();
            return (t, adder.Single(mt => mt.t == t).m, g);
        }).ToList();
    });

    private static ManualConfig CloneConfigExcept(IConfig source, Type? itemTypeToExclude)
    {
        ManualConfig config = ManualConfig.CreateEmpty();
        MethodInfo genericToArray = typeof(Enumerable).GetMethod("ToArray")!;
        Assert.NotNull(genericToArray);
        foreach ((Type item, MethodInfo add, MethodInfo get) in s_methodPairs.Value)
        {
            if (item == itemTypeToExclude) continue;
            object? retVal = get.Invoke(source, []);
            retVal = genericToArray.MakeGenericMethod(item).Invoke(null, [retVal]);
            add.Invoke(config, [retVal]);
        }
        // Statements in IConfig order
        //config.ConfigAnalysisConclusion not settable on ManualConfig
        config.Options = source.Options;
        config.CultureInfo = source.CultureInfo!;
        config.ArtifactsPath = source.ArtifactsPath;
        config.UnionRule = source.UnionRule;
        config.SummaryStyle = source.SummaryStyle;
        config.CategoryDiscoverer = source.CategoryDiscoverer!;
        config.Orderer = source.Orderer!;
        config.BuildTimeout = source.BuildTimeout;
        return config;
    }

    private static void AssertConfigEqual(IConfig expected, IConfig actual)
    {
        foreach ((_, _, MethodInfo get) in s_methodPairs.Value)
        {
            object? retLeft = get.Invoke(expected, []);
            object? retRight = get.Invoke(actual, []);
            Assert.Equal(retLeft, retRight);
        }
        // Statements in IConfig order
        Assert.Equal(expected.Options, actual.Options);
        Assert.Equal(expected.CultureInfo, actual.CultureInfo);
        Assert.Equal(expected.ArtifactsPath, actual.ArtifactsPath);
        Assert.Equal(expected.UnionRule, actual.UnionRule);
        Assert.Equal(expected.SummaryStyle, actual.SummaryStyle);
        Assert.Equal(expected.CategoryDiscoverer, actual.CategoryDiscoverer);
        Assert.Equal(expected.Orderer, actual.Orderer);
        Assert.Equal(expected.BuildTimeout, actual.BuildTimeout);
    }

    private static ManualConfig CreateSourceConfig()
        => CloneConfigExcept(DefaultConfig.Instance, typeof(IAnalyser))
            .AddDiagnoser(ThreadingDiagnoser.Default,
                          new DisassemblyDiagnoser(new DisassemblyDiagnoserConfig()))
            .AddAnalyser(RuntimeErrorAnalyser.Default, ZeroMeasurementAnalyser.Default,
                         BaselineCustomAnalyzer.Default)
            .AddJob(Job.Dry, Job.VeryLongRun)
            .AddHardwareCounters(HardwareCounter.BranchInstructionRetired,
                                 HardwareCounter.BranchInstructions)
            .AddFilter(new SimpleFilter(b => true))
            .AddLogicalGroupRules(BenchmarkLogicalGroupRule.ByCategory);

    [SuppressMessage("Performance", "CA1815:Override equals and operator equals on value types", Justification = "Not needed.")]
    private struct TestCaseData
    {
        public ManualConfig ExpectedNeedsAdd;
        public IConfig IConfigRemove;
        public ManualConfig ManualConfigRemove;
        public IConfig IConfigReplace;
        public ManualConfig ManualConfigReplace;
    }

    private static void RunReplaceTestCase(Type itemType, Func<TestCaseData, TestCaseData> itemTestHandler)
    {
        TestCaseData input;
        ManualConfig config = CreateSourceConfig();
        input.IConfigRemove = CloneConfigExcept(config, null);
        input.ManualConfigRemove = CloneConfigExcept(config, null);
        input.IConfigReplace = CloneConfigExcept(config, null);
        input.ManualConfigReplace = CloneConfigExcept(config, null);
        input.ExpectedNeedsAdd = CloneConfigExcept(config, itemType);
        ManualConfig expectedRemove = CloneConfigExcept(config, itemType);
        TestCaseData actual = itemTestHandler(input);
        Assert.NotSame(actual.IConfigRemove, input.IConfigRemove);
        Assert.NotSame(actual.IConfigReplace, input.IConfigReplace);
        Assert.Same(actual.ManualConfigRemove, input.ManualConfigRemove);
        Assert.Same(actual.ManualConfigReplace, input.ManualConfigReplace);
        AssertConfigEqual(expectedRemove, actual.IConfigRemove);
        AssertConfigEqual(expectedRemove, actual.ManualConfigRemove);
        AssertConfigEqual(actual.ExpectedNeedsAdd, actual.IConfigReplace);
        AssertConfigEqual(actual.ExpectedNeedsAdd, actual.ManualConfigReplace);
    }

    [Fact]
    public void ReplaceExporters_Succeeds()
    {
        RunReplaceTestCase(typeof(IExporter), input =>
        {
            var replacements = new IExporter[] { JsonExporter.Brief, XmlExporter.Brief };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddExporter(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceExporters();
            result.IConfigReplace = input.IConfigReplace.ReplaceExporters(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceExporters();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceExporters(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceLoggers_Succeeds()
    {
        RunReplaceTestCase(typeof(ILogger), input =>
        {
            var replacements = new ILogger[] { ConsoleLogger.Unicode, new LogCapture() };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddLogger(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceLoggers();
            result.IConfigReplace = input.IConfigReplace.ReplaceLoggers(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceLoggers();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceLoggers(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceDiagnosers_Succeeds()
    {
        RunReplaceTestCase(typeof(IDiagnoser), input =>
        {
            var replacements = new IDiagnoser[] { new MemoryDiagnoser(new MemoryDiagnoserConfig()) };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddDiagnoser(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceDiagnosers();
            result.IConfigReplace = input.IConfigReplace.ReplaceDiagnosers(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceDiagnosers();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceDiagnosers(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceAnalysers_Succeeds()
    {
        RunReplaceTestCase(typeof(IAnalyser), input =>
        {
            var replacements = new IAnalyser[] { EnvironmentAnalyser.Default, OutliersAnalyser.Default };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddAnalyser(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceAnalysers();
            result.IConfigReplace = input.IConfigReplace.ReplaceAnalysers(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceAnalysers();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceAnalysers(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceJobs_Succeeds()
    {
        RunReplaceTestCase(typeof(Job), input =>
        {
            var replacements = new Job[] { Job.Dry, Job.ShortRun };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddJob(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceJobs();
            result.IConfigReplace = input.IConfigReplace.ReplaceJobs(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceJobs();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceJobs(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceValidators_Succeeds()
    {
        RunReplaceTestCase(typeof(IValidator), input =>
        {
            var replacements = new IValidator[] { ShadowCopyValidator.DontFailOnError };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddValidator(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceValidators();
            result.IConfigReplace = input.IConfigReplace.ReplaceValidators(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceValidators();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceValidators(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceHardwareCounters_Succeeds()
    {
        RunReplaceTestCase(typeof(HardwareCounter), input =>
        {
            var replacements = new HardwareCounter[] { HardwareCounter.BranchMispredictions, HardwareCounter.BranchMispredictsRetired, HardwareCounter.CacheMisses };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddHardwareCounters(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceHardwareCounters();
            result.IConfigReplace = input.IConfigReplace.ReplaceHardwareCounters(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceHardwareCounters();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceHardwareCounters(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceFilters_Succeeds()
    {
        RunReplaceTestCase(typeof(IFilter), input =>
        {
            var replacements = new IFilter[] { new NameFilter(n => true), new WhatifFilter() };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddFilter(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceFilters();
            result.IConfigReplace = input.IConfigReplace.ReplaceFilters(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceFilters();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceFilters(replacements);
            return result;
        });
    }

    [Fact]
    public void ReplaceLogicalGroupRules_Succeeds()
    {
        RunReplaceTestCase(typeof(BenchmarkLogicalGroupRule), input =>
        {
            var replacements = new BenchmarkLogicalGroupRule[] { BenchmarkLogicalGroupRule.ByJob, BenchmarkLogicalGroupRule.ByMethod };
            TestCaseData result;
            result.ExpectedNeedsAdd = input.ExpectedNeedsAdd.AddLogicalGroupRules(replacements);
            result.IConfigRemove = input.IConfigRemove.ReplaceLogicalGroupRules();
            result.IConfigReplace = input.IConfigReplace.ReplaceLogicalGroupRules(replacements);
            result.ManualConfigRemove = input.ManualConfigRemove.ReplaceLogicalGroupRules();
            result.ManualConfigReplace = input.ManualConfigReplace.ReplaceLogicalGroupRules(replacements);
            return result;
        });
    }
}
