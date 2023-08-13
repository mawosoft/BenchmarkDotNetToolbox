// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Xunit;
using Xunit.Abstractions;

namespace Mawosoft.Extensions.BenchmarkDotNet.Tests
{
    public class BDNVersionTests
    {
        private readonly ITestOutputHelper _testOutput;
        public BDNVersionTests(ITestOutputHelper testOutput) => _testOutput = testOutput;

        [Fact]
        public void BDNVersionNightlyOrStable()
        {
            string myName = typeof(BDNVersionTests).Assembly.FullName ?? string.Empty;
            Assert.Matches(@"Tests\.(Stable|Nightly)BDN", myName);
            bool nightly = myName.Contains("NightlyBDN");
            CheckAssembly(typeof(BenchmarkCase).Assembly, nightly);
            CheckAssembly(typeof(BenchmarkAttribute).Assembly, nightly);

            void CheckAssembly(Assembly assembly, bool nightly)
            {
                // BDN now uses proper prerelease labels
                _testOutput.WriteLine(assembly.FullName);
                AssemblyInformationalVersionAttribute v = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!;
                Assert.NotNull(v);
                _testOutput.WriteLine("Informational Version: " + v.InformationalVersion);
                Assert.Equal(nightly, v.InformationalVersion.Contains('-'));
            }
        }

        [Fact]
        public void BDNAssemblyGetTypes()
        {
            List<Type> types = new();
            Assembly assembly = typeof(BenchmarkCase).Assembly;
            try
            {
                types.AddRange(assembly.GetTypes());
            }
            catch (ReflectionTypeLoadException rtle)
            {
                // BDN [0.13.2.1930, 0.13.2.2029) used the Arm64 disassembler package Gee.External.Capstone 2.2.0. Unlike BDN itself,
                // the package was not strong-named, therfore causing a ReflectionTypeLoadException on NetFx (strong-named assembly required).
                // BDN >= 0.13.2.2029 uses the now strong-named Gee.External.Capstone 2.3.0.
                _testOutput.WriteLine(rtle.Message);
                _testOutput.WriteLine($"# of LoaderExceptions: {rtle.LoaderExceptions.Length}");
                _testOutput.WriteLine(string.Join(Environment.NewLine, rtle.LoaderExceptions.GroupBy(e => e?.Message, (k, g) => $"{g.Count()}x {k}")));
                types.AddRange(rtle.Types.Where(t => t != null).Select(t => t!));
#if NETFRAMEWORK
                Version v = assembly.GetName().Version;
                Assert.True(v >= new Version("0.13.2.1930") && v < new Version("0.13.2.2029"), "ReflectionTypeLoadException only tolerated for BDN [0.13.2.1930, 0.13.2.2029)");
#else
                Assert.Fail("ReflectionTypeLoadException only tolerated on .NET Framework.");
#endif

            }
            Assert.NotEmpty(types);
        }
    }
}
