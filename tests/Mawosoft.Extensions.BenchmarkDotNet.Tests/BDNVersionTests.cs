// Copyright (c) 2021-2022 Matthias Wolf, Mawosoft.

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
                _testOutput.WriteLine(assembly.FullName);
                int? revision = assembly.GetName().Version?.Revision;
                Assert.NotNull(revision);
                Assert.Equal(nightly, revision != 0);
            }
        }
    }
}
