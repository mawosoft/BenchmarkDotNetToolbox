// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using BenchmarkDotNet.Configs;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>
    /// Initializes a user-defined selection of Job columns.
    /// See <see cref="JobColumnSelectionProvider"/> for details.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Assembly)]
    public class JobColumnSelectionAttribute : Attribute, IConfigSource
    {
        /// <summary>
        /// Initializes a user-defined selection of Job columns.
        /// See <see cref="JobColumnSelectionProvider(string, bool)"/> for details.
        /// </summary>
        public JobColumnSelectionAttribute(string filterExpression, bool showHiddenValuesInLegend = true)
            => Config = ManualConfig.CreateEmpty().AddColumnProvider(
                new JobColumnSelectionProvider(filterExpression, showHiddenValuesInLegend));
        public IConfig Config { get; }
    }
}
