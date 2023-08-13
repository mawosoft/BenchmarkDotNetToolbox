// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Reflection;
using BenchmarkDotNet.Toolchains;
using BenchmarkDotNet.Toolchains.Results;

namespace Mawosoft.Extensions.BenchmarkDotNet.ApiCompat;

internal static class GenerateResultWrapper
{
    public static GenerateResult Create(
        ArtifactsPaths artifactsPaths,
        bool isGenerateSuccess,
        Exception? generateException,
        IReadOnlyCollection<string> artifactsToCleanup)
    {
        return new GenerateResult(artifactsPaths, isGenerateSuccess, generateException!, artifactsToCleanup);
    }

    public static GenerateResult Success()
        => Create(ArtifactsPaths.Empty, true, null, Array.Empty<string>());
}
