// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>Internal class used by <see cref="JobColumnSelectionProvider"/></summary>
    internal class JobCharacteristicColumnWithLegend : IColumn
    {
        private readonly IColumn _inner;

        public JobCharacteristicColumnWithLegend(IColumn inner, string legend)
        {
            _inner = inner;
            Legend = legend;
        }

        public string Id => _inner.Id;
        public string ColumnName => _inner.ColumnName;
        public bool AlwaysShow => _inner.AlwaysShow;
        public ColumnCategory Category => _inner.Category;
        public int PriorityInCategory => _inner.PriorityInCategory;
        public bool IsNumeric => _inner.IsNumeric;
        public UnitType UnitType => _inner.UnitType;
        public string Legend { get; }
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => _inner.GetValue(summary, benchmarkCase);
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => _inner.GetValue(summary, benchmarkCase, style);
        public bool IsAvailable(Summary summary) => _inner.IsAvailable(summary);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => _inner.IsDefault(summary, benchmarkCase);
    }
}
