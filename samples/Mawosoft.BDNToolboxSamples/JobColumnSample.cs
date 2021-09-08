// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Portability;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using BenchmarkDotNet.Validators;
using Mawosoft.BenchmarkDotNetToolbox;

namespace Mawosoft.BDNToolboxSamples
{
    public class JobColumnSampleBenchmarks
    {
        public class UsingGlobalConfigForEverything
        {
            [Benchmark]
            public int Method1() => new Random().Next();
            [Benchmark]
            public int Method2() => new Random().Next() + new Random().Next();
        }
    }
}
