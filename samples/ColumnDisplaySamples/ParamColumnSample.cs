// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Mawosoft.BenchmarkDotNetToolbox;

namespace ColumnDisplaySamples
{
    public abstract class ParamColumnSample : SampleBase
    {
        public override string SampleGroupDescription => "Param Column Sample";

        public class SampleConfig : SampleConfigBase
        {
            public SampleConfig() : base() =>
                AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.DontLogOutput));
        }

        [Benchmark]
        [Arguments("fooval1", "barval1")]
        [Arguments("fooval2", "barval2")]
        public int Method1(string fooArg, string barArg) => new Random().Next() + fooArg.Length + barArg.Length;
        [Benchmark]
        [Arguments("fooval1", "bazval1")]
        [Arguments("fooval2", "bazval2")]
        public int Method2(string fooArg, string bazArg) => new Random().Next() + fooArg.Length + bazArg.Length;
        [Benchmark]
        [Arguments("fooval1", "buzzval1")]
        [Arguments("fooval2", "buzzval2")]
        public int Method3(string fooArg, string buzzArg) => new Random().Next() + fooArg.Length + buzzArg.Length;
    }

    [Config(typeof(SampleConfig))]
    public class ParamColumnSample_BDNDefault : ParamColumnSample
    {
        public override string SampleVariantDescription => BenchmarkDotNetDefault;
    }

    [Config(typeof(SampleConfig))]
    public class ParamColumnSample_CombinedParamsColumn_Default : ParamColumnSample
    {
        public override string SampleVariantDescription => "with CombinedParamsColumn() // default formatting";
        // Same config as above, but now with CombinedParamsColumn and a wider column width since all params
        // are displayed in the same column.
        // - ReplaceColumnCategory is one of the new extension methods in
        //   Mawosoft.BenchmarkDotNetToolbox.ConfigExtensions.
        public new class SampleConfig : ParamColumnSample.SampleConfig
        {
            public SampleConfig() : base() =>
                this.ReplaceColumnCategory(new CombinedParamsColumn())
                    .WithSummaryStyle(SummaryStyle.WithMaxParameterColumnWidth(40));
        }

    }

    [Config(typeof(SampleConfig))]
    public class ParamColumnSample_CombinedParamsColumn_Custom : ParamColumnSample
    {
        public override string SampleVariantDescription
            => @"with CombinedParamsColumn(formatNameValue: ""{1}"", separator: ""; "")";
        // Same config as above, but now with a custom formatting in CombinedParamsColumn.
        public new class SampleConfig : ParamColumnSample.SampleConfig
        {
            public SampleConfig() : base() =>
                this.ReplaceColumnCategory(new CombinedParamsColumn(formatNameValue: "{1}", separator: "; "))
                    .WithSummaryStyle(SummaryStyle.WithMaxParameterColumnWidth(40));
        }
    }

    [Config(typeof(SampleConfig))]
    public class ParamColumnSample_RecyclableParamsColumnProvider_Default : ParamColumnSample
    {
        public override string SampleVariantDescription
            => "with RecyclableParamsColumnProvider() // default settings";
        // Same config as above, but now with RecyclableParamsColumnProvider.
        public new class SampleConfig : ParamColumnSample.SampleConfig
        {
            public SampleConfig() : base() =>
                this.ReplaceColumnCategory(new RecyclableParamsColumnProvider());
        }
    }

    [Config(typeof(SampleConfig))]
    public class ParamColumnSample_RecyclableParamsColumnProvider_Custom : ParamColumnSample
    {
        public override string SampleVariantDescription
            => "with RecyclableParamsColumnProvider(tryKeepParamName: false)";
        // Same config as above, but now with customized RecyclableParamsColumnProvider.
        public new class SampleConfig : ParamColumnSample.SampleConfig
        {
            public SampleConfig() : base() =>
                this.ReplaceColumnCategory(new RecyclableParamsColumnProvider(tryKeepParamName: false));
        }
    }
}
