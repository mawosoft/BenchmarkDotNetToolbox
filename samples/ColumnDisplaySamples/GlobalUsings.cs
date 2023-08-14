// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

global using System;
global using System.Collections.Generic;
global using System.Diagnostics.CodeAnalysis;
global using System.IO;
global using System.Linq;
global using BenchmarkDotNet.Analysers;
global using BenchmarkDotNet.Attributes;
global using BenchmarkDotNet.Columns;
global using BenchmarkDotNet.Configs;
global using BenchmarkDotNet.Environments;
global using BenchmarkDotNet.Exporters;
global using BenchmarkDotNet.Jobs;
global using BenchmarkDotNet.Loggers;
global using BenchmarkDotNet.Reports;
global using BenchmarkDotNet.Running;
global using BenchmarkDotNet.Toolchains.CsProj;
global using BenchmarkDotNet.Toolchains.DotNetCli;
global using BenchmarkDotNet.Toolchains.InProcess.Emit;
global using Mawosoft.Extensions.BenchmarkDotNet;
