---
uid: Mawosoft.Extensions.BenchmarkDotNet.ParamWrapper`1
syntax:
    typeParameters:
    - id: T
      description: The type of the wrapped value.
example:
- *content
seealso:
- linkType: HRef
  linkId: https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/tree/master/samples
  altText: Column Display Samples on GitHub
---

```csharp
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

```

##### Sample Output

<pre>
// *** ParamWrapper Sample ***

Job=Dry  Toolchain=InProcessEmitToolchain  IterationCount=1  
LaunchCount=1  RunStrategy=ColdStart  UnrollFactor=1  
WarmupCount=1  

|     Method |                  input |     Mean | Error |
|----------- |----------------------- |---------:|------:|
| NotWrapped | System.IO.MemoryStream | 452.0 μs |    NA |
| NotWrapped | System.IO.MemoryStream | 203.7 μs |    NA |
|    Wrapped |             big stream | 449.4 μs |    NA |
|    Wrapped |           small stream | 213.8 μs |    NA |

  input : Value of the 'input' parameter
  Mean  : Arithmetic mean of all measurements
  Error : Half of 99.9% confidence interval
  1 μs  : 1 Microsecond (0.000001 sec)
</pre>

---
uid: Mawosoft.Extensions.BenchmarkDotNet.ParamWrapper`1.#ctor(`0,System.String)
syntax:
    parameters:
    - id: value
      description: The parameter or argument value to wrap.
    - id: displayText
      description: The associated text to display in logs and summaries.
---
