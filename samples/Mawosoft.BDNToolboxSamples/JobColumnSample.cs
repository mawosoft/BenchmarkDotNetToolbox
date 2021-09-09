// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.CsProj;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Mawosoft.BenchmarkDotNetToolbox;

namespace Mawosoft.BDNToolboxSamples
{
    public abstract class JobColumnSample1 : SampleBase
    {
        public override string SampleGroupDescription => "JobColumn Sample 1: Jobs with different run characteristics";

        public class JobColumnSample1Config : SampleConfigBase
        {
            public JobColumnSample1Config() : base() =>
                AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.DontLogOutput),
                       Job.Default.WithToolchain(InProcessEmitToolchain.DontLogOutput));
        }

        [Benchmark]
        public int Method1() => new Random().Next();
        [Benchmark]
        public int Method2() => new Random().Next() + new Random().Next();
    }

    [Config(typeof(JobColumnSample1Config))]
    public class JobColumnSample1Default : JobColumnSample1
    {
        public override string SampleVariantDescription => BenchmarkDotNetDefault;
    }

    [Config(typeof(JobColumnSample1MawosoftConfig))]
    public class JobColumnSample1Mawosoft : JobColumnSample1
    {
        public override string SampleVariantDescription => "with JobColumnSelectionProvider";

        // Same config as above, but now with JobColumnSelectionProvider
        // - "-all +Job" hides all job columns, then unhides the Job name column.
        //   Alternatively, we could also have used just  "-Run".
        // - ReplaceColumnCategory is one of the new extension methods in ManualConfigExtensions.
        public class JobColumnSample1MawosoftConfig : JobColumnSample1Config
        {
            public JobColumnSample1MawosoftConfig() : base() =>
                this.ReplaceColumnCategory(ColumnCategory.Job,
                    new JobColumnSelectionProvider("-all +Job", showHiddenValuesInLegend: true));
        }
    }

    public abstract class JobColumnSample2 : SampleBase
    {
        public override string SampleGroupDescription => "JobColumn Sample 2: Jobs with different target frameworks";

        public class JobColumnSample2Config : SampleConfigBase
        {
            public JobColumnSample2Config() : base() =>
                AddJob(
                    Job.Dry.WithRuntime(ClrRuntime.Net48).WithToolchain(CsProjClassicNetToolchain.Net48)
                        .AsBaseline().UnfreezeCopy(),
                    Job.Dry.WithRuntime(CoreRuntime.Core31).WithToolchain(CsProjCoreToolchain.NetCoreApp31).UnfreezeCopy(),
                    Job.Dry.WithRuntime(CoreRuntime.Core50).WithToolchain(CsProjCoreToolchain.NetCoreApp50).UnfreezeCopy()
                    );
        }

        [Benchmark]
        public int Method1() => new Random().Next();
        [Benchmark]
        public int Method2() => new Random().Next() + new Random().Next();
    }

    [Config(typeof(JobColumnSample2Config))]
    public class JobColumnSample2Default : JobColumnSample2
    {
        public override string SampleVariantDescription => BenchmarkDotNetDefault;
    }

    [Config(typeof(JobColumnSample2MawosoftConfig))]
    public class JobColumnSample2Mawosoft : JobColumnSample2
    {
        public override string SampleVariantDescription => "with JobColumnSelectionProvider";

        // Same config as above, but now with JobColumnSelectionProvider
        public class JobColumnSample2MawosoftConfig : JobColumnSample2Config
        {
            public JobColumnSample2MawosoftConfig() : base() =>
                this.ReplaceColumnCategory(ColumnCategory.Job,
                    new JobColumnSelectionProvider("-Job -Toolchain", showHiddenValuesInLegend: false));
        }
    }
}
