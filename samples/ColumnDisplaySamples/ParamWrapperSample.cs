// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace ColumnDisplaySamples;

[Config(typeof(SampleConfig))]
public class ParamWrapperSample : SampleBase
{
    internal override string SampleGroupDescription => "ParamWrapper Sample";
    internal override string SampleVariantDescription => "";

    internal class SampleConfig : SampleConfigBase
    {
        public SampleConfig() : base()
            => AddJob(Job.Dry.WithToolchain(InProcessEmitToolchain.DontLogOutput))
                .WithSummaryStyle(SummaryStyle.WithMaxParameterColumnWidth(25));

    }

    public IEnumerable<MemoryStream> ArgumentsSource_NotWrapped()
    {
        yield return new MemoryStream(50);
        yield return new MemoryStream(500);
    }

    public IEnumerable<ParamWrapper<MemoryStream>> ArgumentsSource_Wrapped()
    {
        yield return new ParamWrapper<MemoryStream>(new MemoryStream(50), "small stream");
        yield return new ParamWrapper<MemoryStream>(new MemoryStream(500), "big stream");
    }

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentsSource_NotWrapped))]
    public void NotWrapped(MemoryStream input)
    {
        input.Seek(0, SeekOrigin.Begin);
        byte[] buffer = new byte[10];
        while (input.Read(buffer, 0, buffer.Length) == buffer.Length) { }
    }

    [Benchmark]
    [ArgumentsSource(nameof(ArgumentsSource_Wrapped))]
    public void Wrapped(ParamWrapper<MemoryStream> input)
    {
        MemoryStream stream = input.Value;
        stream.Seek(0, SeekOrigin.Begin);
        byte[] buffer = new byte[10];
        while (stream.Read(buffer, 0, buffer.Length) == buffer.Length) { }
    }
}
