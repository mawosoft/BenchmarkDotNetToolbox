// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

using System.Diagnostics.CodeAnalysis;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Parameters;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;

namespace Mawosoft.Extensions.BenchmarkDotNet;

/// <summary>
/// Internal class used by <see cref="RecyclableParamsColumnProvider"/>
/// </summary>
internal class RecyclableParamColumn : IColumn
{
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression",
        Justification = "False warning for IDE0032 supression.")]
    [SuppressMessage("Style", "IDE0032:Use auto property")]
    private readonly int _paramIndex;
    private readonly bool _isRealName;
    public RecyclableParamColumn(int paramIndex, string columnName, bool isRealName)
    {
        _paramIndex = paramIndex;
        _isRealName = isRealName;
        ColumnName = columnName;
    }

    public string Id => nameof(RecyclableParamColumn) + "." + _paramIndex;
    public string ColumnName { get; }
    public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase) => GetValue(summary, benchmarkCase, summary.Style);
    public bool IsAvailable(Summary summary) => true;
    public bool AlwaysShow => true;
    public ColumnCategory Category => ColumnCategory.Params;
    public int PriorityInCategory => _paramIndex;
    public override string ToString() => ColumnName;
    public bool IsNumeric => false;
    public UnitType UnitType => UnitType.Dimensionless;
    public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) =>
        benchmarkCase.Parameters.Items.ElementAtOrDefault(_paramIndex)?.ToDisplayText(style) ??
        ParameterInstance.NullParameterTextRepresentation;
    public string Legend => _isRealName
        ? $"Value of the '{ColumnName}' parameter"
        : $"Value of the parameter at position {_paramIndex + 1}";
}
