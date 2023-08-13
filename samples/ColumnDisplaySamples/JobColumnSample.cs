// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace ColumnDisplaySamples;

public abstract class JobColumnSample1_RunModes : SampleBase
{
    public override string SampleGroupDescription
        => "Job Column Sample 1: Jobs with different run characteristics";

    public class SampleConfig : SampleConfigBase
    {
        public SampleConfig() : base() =>
            AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.DontLogOutput),
                   Job.Default.WithToolchain(InProcessEmitToolchain.DontLogOutput));
    }

    [Benchmark]
    public int Method1() => new Random().Next(10);
    [Benchmark]
    public int Method2() => new Random().Next(20);
}

[Config(typeof(SampleConfig))]
public class JobColumnSample1_RunModes_BDNDefault : JobColumnSample1_RunModes
{
    public override string SampleVariantDescription => BenchmarkDotNetDefault;
}

[Config(typeof(SampleConfig))]
public class JobColumnSample1_RunModes_JobColumnSelectionProvider : JobColumnSample1_RunModes
{
    public override string SampleVariantDescription
        => @"with JobColumnSelectionProvider(""-all +Job"", showHiddenValuesInLegend: true)";

    // Same config as above, but now with JobColumnSelectionProvider
    // - "-all +Job" hides all job columns, then unhides the Job name column.
    //   Alternatively, we could also have used just  "-Run".
    // - ReplaceColumnCategory is one of the new extension methods in this library.
    public new class SampleConfig : JobColumnSample1_RunModes.SampleConfig
    {
        public SampleConfig() : base() =>
            this.ReplaceColumnCategory(
                new JobColumnSelectionProvider("-all +Job", showHiddenValuesInLegend: true));
    }
}

public abstract class JobColumnSample2_Runtimes : SampleBase
{
    public override string SampleGroupDescription
        => "Job Column Sample 2: Jobs with different target frameworks";

    public class SampleConfig : SampleConfigBase
    {
        // There are easier ways to specify Runtime and ToolChain for a job.
        // This however mimics the way jobs are created from the command line option
        //     --runtimes net48 net6.0 net7.0
        // with the first specified runtime becoming the baseline.
        // But we are using Job.Dry as the base job here for minimal duration.
        public SampleConfig() : base() =>
            AddJob(
                Job.Dry.WithRuntime(ClrRuntime.Net48)
                       .WithToolchain(CsProjClassicNetToolchain.From(
                           "net48"))
                       .AsBaseline().UnfreezeCopy(),
                Job.Dry.WithRuntime(CoreRuntime.Core60)
                       .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings(
                           "net6.0", null!, "net6.0")))
                       .UnfreezeCopy(),
                Job.Dry.WithRuntime(CoreRuntime.Core60)
                       .WithToolchain(CsProjCoreToolchain.From(new NetCoreAppSettings(
                           "net7.0", null!, "net7.0")))
                       .UnfreezeCopy()
                );
    }

    [Benchmark]
    public int Method1() => new Random().Next(10);
    [Benchmark]
    public int Method2() => new Random().Next(20);
}

[Config(typeof(SampleConfig))]
public class JobColumnSample2_Runtimes_BDNDefault : JobColumnSample2_Runtimes
{
    public override string SampleVariantDescription => BenchmarkDotNetDefault;
}

[Config(typeof(SampleConfig))]
public class JobColumnSample2_Runtimes_JobColumnSelectionProvider : JobColumnSample2_Runtimes
{
    public override string SampleVariantDescription
        => @"with JobColumnSelectionProvider(""-Job -Toolchain"", showHiddenValuesInLegend: false)";

    // Same config as above, but now with JobColumnSelectionProvider
    public new class SampleConfig : JobColumnSample2_Runtimes.SampleConfig
    {
        public SampleConfig() : base() =>
            this.ReplaceColumnCategory(
                new JobColumnSelectionProvider("-Job -Toolchain", showHiddenValuesInLegend: false));
    }
}
