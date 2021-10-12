---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos
---
As a minor benefit, `BenchmarkRunInfos` simplifies the use of @"BenchmarkDotNet.Running.BenchmarkConverter":
- You can store the global config to use in the @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config"
property instead of passing it as an argument in each call to a conversion method.
- The results of all conversions are automatically collected and by calling @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.RunAll"
afterwards, you can execute them.
- `BenchmarkRunInfos` also provides two additional conversion methods for converting an entire @"System.Reflection.Assembly"
and for converting methods by their name instead of their @"System.Reflection.MethodInfo".

However, the main purpose of `BenchmarkRunInfos` is the support of debugging scenarios:
- Specifying an @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" will override *all*
@"BenchmarkDotNet.Jobs.Job" configurations, regardless whether they are defined globally or locally,
as an explit, default or mutator job, via custom config or via attribute annotation.
- Even if your config contains multiple jobs or you have annotated your benchmark classes with multiple jobs
(e.g. via `[DryJob, ShortRunJob]` or similar attributes), each converted benchmark method will only be
executed with the override job.
- To achieve that, you don't have to change the coding of your configurations, just add a few extra statements
to your `Main` method that will get executed under certain conditions.

> [!NOTE]
> It may seem that instead of using @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos" and
> @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" in a debugging scenario,
> you could just create a different global config and set its @"BenchmarkDotNet.Configs.ManualConfig.UnionRule"
> property to @"BenchmarkDotNet.Configs.ConfigUnionRule.AlwaysUseGlobal?displayProperty=nameWithType".
>
> That however will have **no effect at all**, because BenchmarkDotNet always takes the UnionRule from the local config.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos
seealso:
- linkType: HRef
  linkId: https://github.com/mawosoft/Mawosoft.Extensions.BenchmarkDotNet/tree/master/samples
  altText: BenchmarkRunInfos Sample on GitHub
example:
- *content
---

```csharp
public static void Main(string[] args)
{
    // Your customized global config (Default used here for simplicity)
    ManualConfig config = ManualConfig.Create(DefaultConfig.Instance);
    if (Debugger.IsAttached && args.Length == 0)
    {
        // Debugging scenario
        BenchmarkRunInfos runInfos = new();
        runInfos.Config = config
            .WithOption(ConfigOptions.DisableOptimizationsValidator, true);
        runInfos.OverrideJob = BenchmarkRunInfos.FastInProcessJob;
        // Pick only the methods you want to debug
        runInfos.ConvertMethodsToBenchmarks(typeof(MyClass1), "Method1", "Method2");
        runInfos.ConvertMethodsToBenchmarks(typeof(MyClass2), "Method3");
        runInfos.RunAll();
    }
    else
    {
        // Regular scenario
        BenchmarkRunner.Run(typeof(Program).Assembly, config, args);
    }
}
```

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.#ctor(BenchmarkDotNet.Configs.IConfig,BenchmarkDotNet.Jobs.Job)
syntax:
    parameters:
    - id: globalConfig
      description: The global config to use for conversions, or `null` to use BenchmarkDotNet\'s default config.
    - id: overrideJob
      description: The override job to use, or `null` to not use an override job at all.
remarks: *content
---
You can change the @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" and @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob"
properties at any time. This will not affect any benchmarks already converted, only subsequent benchmark conversions.

The predefined @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.FastInProcessJob" is the best choice for debugging,
but you can also define your own job if you prefer.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.FastInProcessJob
syntax:
    return:
        description: The predefined Job instance.
remarks: "`FastInProcessJob` is equivalent to `Job.Dry.WithToolchain(InProcessEmitToolchain.Instance)`."
---

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config
syntax:
    return:
        description: The global config to use for conversions, or `null` to use BenchmarkDotNet's default config.
remarks: *content
---
You can change the @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config"
property at any time. This will not affect any benchmarks already converted, only subsequent benchmark conversions.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob
syntax:
    return:
        description: The override job to use, or `null` to not use an override job at all.
remarks: *content
---
You can change the @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob"
property at any time. This will not affect any benchmarks already converted, only subsequent benchmark conversions.

The predefined @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.FastInProcessJob" is the best choice for debugging,
but you can also define your own job if you prefer.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Items
syntax:
    return:
        description: The read-only collection of the converted benchmarks.
---

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Count
syntax:
    return:
        description: The number of converted benchmarks.
---

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Item(System.Int32)
syntax:
    parameters:
    - id: index
      description: The zero-based index of the element to get.
    return:
      description: The element at the specified index.
---

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Add(BenchmarkDotNet.Running.BenchmarkRunInfo)
syntax:
    parameters:
    - id: benchmarkRunInfo
      description: The @"BenchmarkDotNet.Running.BenchmarkRunInfo" element to be added.
---
> [!Note]
> Since @"BenchmarkDotNet.Running.BenchmarkRunInfo" already contains converted benchmarks, an eventual
> @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" will not be applied.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.AddRange(System.Collections.Generic.IEnumerable{BenchmarkDotNet.Running.BenchmarkRunInfo})
syntax:
    parameters:
    - id: benchmarkRunInfos
      description: The collection of @"BenchmarkDotNet.Running.BenchmarkRunInfo" elements to be added.
---
> [!Note]
> Since @"BenchmarkDotNet.Running.BenchmarkRunInfo" already contains converted benchmarks, an eventual
> @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" will not be applied.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Clear(System.Boolean)
syntax:
    parameters:
    - id: dispose
      description: "`true` to dispose all benchmarks, `false` to only remove them from the list without disposal."
---

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.ConvertAssemblyToBenchmarks(System.Reflection.Assembly)
syntax:
    parameters:
    - id: assembly
      description: The assembly containing the benchmark classes and methods.
---
An eventual @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" and
@"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" will be applied.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.ConvertMethodsToBenchmarks(System.Type,System.Reflection.MethodInfo[])
syntax:
    parameters:
    - id: containingType
      description: "The `Type` of the class containing the benchmark methods."
    - id: benchmarkMethods
      description: "An array of `MethodInfo` objects describing the benchmark methods."
---
An eventual @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" and
@"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" will be applied.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.ConvertMethodsToBenchmarks(System.Type,System.String[])
syntax:
    parameters:
    - id: containingType
      description: "The `Type` of the class containing the benchmark methods."
    - id: benchmarkMethodNames
      description: An array of strings containing the names of the benchmark methods.
---
An eventual @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" and
@"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" will be applied.

> [!Note]
> The method names must be unambigious, otherwise the conversion will fail.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.ConvertSourceToBenchmarks(System.String)
syntax:
    parameters:
    - id: source
      description: A string containing the C# source code to compile.
---
An eventual @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" and
@"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" will be applied.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.ConvertTypeToBenchmarks(System.Type)
syntax:
    parameters:
    - id: type
      description: "The `Type` of the class containing the benchmark methods."
---
An eventual @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" and
@"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" will be applied.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.ConvertUrlToBenchmarks(System.String)
syntax:
    parameters:
    - id: url
      description: A string containing the Url to read the C# source code from.
---
An eventual @"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.Config" and
@"Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.OverrideJob" will be applied.

---
uid: Mawosoft.Extensions.BenchmarkDotNet.BenchmarkRunInfos.RunAll
syntax:
    return:
      description: "An array of `Summary` objects containing the benchmark results."
---
