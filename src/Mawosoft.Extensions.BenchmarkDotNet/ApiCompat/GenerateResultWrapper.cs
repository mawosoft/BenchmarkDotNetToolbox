// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat
{
    internal static class GenerateResultWrapper
    {
        private static readonly ConstructorInfo? s_ctorStable;
        private static readonly ConstructorInfo? s_ctorNightly;

        static GenerateResultWrapper()
        {
            s_ctorStable = typeof(GenerateResult).GetConstructor(new Type[] {
                typeof(ArtifactsPaths), typeof(bool), typeof(Exception), typeof(IReadOnlyCollection<string>),
                typeof(bool)
            });
            if (s_ctorStable == null)
            {
                s_ctorNightly = typeof(GenerateResult).GetConstructor(new Type[] {
                    typeof(ArtifactsPaths), typeof(bool), typeof(Exception), typeof(IReadOnlyCollection<string>)
                });
            }
        }

        public static GenerateResult Create(
            ArtifactsPaths artifactsPaths,
            bool isGenerateSuccess, Exception?
            generateException,
            IReadOnlyCollection<string> artifactsToCleanup,
            bool noAcknowledgments)
        {
            if (s_ctorStable != null)
            {
                return (GenerateResult)s_ctorStable.Invoke(new object?[] {
                    artifactsPaths, isGenerateSuccess, generateException, artifactsToCleanup, noAcknowledgments
                });
            }
            else if (s_ctorNightly != null)
            {
                return (GenerateResult)s_ctorNightly.Invoke(new object?[] {
                    artifactsPaths, isGenerateSuccess, generateException, artifactsToCleanup
                });
            }
            else
            {
                throw new MissingMethodException(nameof(GenerateResult), ".ctor");
            }
        }

        public static GenerateResult Success()
            => Create(ArtifactsPaths.Empty, true, null, Array.Empty<string>(), false);
    }
}
