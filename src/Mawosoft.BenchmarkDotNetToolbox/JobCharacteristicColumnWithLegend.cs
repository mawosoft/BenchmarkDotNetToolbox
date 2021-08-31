// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    internal class JobCharacteristicColumnWithLegend : IColumn
    {
        private readonly IColumn _inner;
        private readonly string _legend;

        public JobCharacteristicColumnWithLegend(IColumn inner, string legend)
        {
            _inner = inner;
            _legend = legend;
        }

        public string Id => _inner.Id;
        public string ColumnName => _inner.ColumnName;
        public bool AlwaysShow => _inner.AlwaysShow;
        public ColumnCategory Category => _inner.Category;
        public int PriorityInCategory => _inner.PriorityInCategory;
        public bool IsNumeric => _inner.IsNumeric;
        public UnitType UnitType => _inner.UnitType;
        public string Legend => _legend;
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => _inner.GetValue(summary, benchmarkCase);
        public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => _inner.GetValue(summary, benchmarkCase, style);
        public bool IsAvailable(Summary summary) => _inner.IsAvailable(summary);
        public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => _inner.IsDefault(summary, benchmarkCase);
    }
}
