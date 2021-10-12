// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Characteristics;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>
    /// An alternative to <see cref="DefaultColumnProviders.Job"/>, with a user-defined selection of Job columns.
    /// </summary>
    public class JobColumnSelectionProvider : IColumnProvider
    {
        private static readonly Lazy<FilterFactory> s_filterFactory = new();
        private readonly bool _showHiddenValuesInLegend;
        private readonly CharacteristicFilter[] _presentableCharacteristicFilters;

        /// <summary>
        /// Initializes a new instance of the <see cref="JobColumnSelectionProvider"/> class.
        /// </summary>
        /// <remarks>
        /// <para>The filter expressions are processed sequentially. Initially, all columns are visible, just
        /// as with the default provider. The filter <c>"-All +Job"</c> will first hide all job columns and then
        /// unhide the Job name column. <c>"-Run +RunStrategy"</c> will hide the columns of the Run category and
        /// then unhide the RunStrategy column.</para>
        /// <para>Each job column represents a job characteristic. Characteristics are grouped into categories,
        /// which can be specified either by their name or by their type, e.g. <c>Run</c> and <c>RunMode</c> are
        /// equivalent. Available categories are: <c>Environment</c>, <c>Gc</c>, <c>Run</c>,
        /// <c>Infrastructure</c>, <c>Accuracy</c>. For more details about them, see the
        /// <see href="https://benchmarkdotnet.org/articles/configs/jobs.html">BenchmarkDotNet
        /// documentation</see>.</para>
        /// <para>In addition, the alias <c>All</c> refers to all columns, and both <c>Job</c> and <c>Id</c>
        /// can be used for the Job name colum.</para>
        /// <para>All column and category names are case-insensitive.</para>
        /// </remarks>
        public JobColumnSelectionProvider(string filterExpression, bool showHiddenValuesInLegend = true)
        {
            _showHiddenValuesInLegend = showHiddenValuesInLegend;
            _presentableCharacteristicFilters =
                s_filterFactory.Value.CreateCharacteristicFilters(filterExpression)
                .Where(c => c.Column != null).ToArray();
        }

        public IEnumerable<IColumn> GetColumns(Summary summary)
        {
            ColumnFilter[] columnFilters
                = _presentableCharacteristicFilters.Select(c => new ColumnFilter(c, summary)).ToArray();
            if (_showHiddenValuesInLegend)
            {
                int legendColumnIndex = Array.FindIndex(columnFilters, cf => cf.IsVisible);
                if (legendColumnIndex >= 0)
                {
                    int idColumnIndex = Array.FindIndex(columnFilters, cf => cf.Characteristic.Id == "Id");
                    if (idColumnIndex >= 0 && !columnFilters[idColumnIndex].IsHidden)
                    {
                        legendColumnIndex = idColumnIndex;
                    }

                    IEnumerable<ColumnFilter> hiddenColumns =
                        columnFilters.Where(cf => cf.IsHidden && cf.Characteristic.Id != "Id");
                    if (hiddenColumns.Any())
                    {
                        int paddedJobNameLength = summary.BenchmarksCases.Max(b => b.Job.ResolvedId.Length) + 4;
                        string legend = (legendColumnIndex == idColumnIndex ? "Job name" : "Job characteristic")
                            + ". Some job columns have been hidden:" + Environment.NewLine
                            + string.Join(Environment.NewLine,
                                summary.BenchmarksCases.Select(b =>
                                    b.Job.ResolvedId.PadLeft(paddedJobNameLength) + ": " 
                                    + string.Join(", ", hiddenColumns.Select(cf =>
                                        cf.Characteristic.Id + "=" + cf.Column.GetValue(summary, b)))).Distinct());
                        columnFilters[legendColumnIndex].AddLegend(legend);
                    }
                    else if (columnFilters[idColumnIndex].IsHidden)
                    {
                        string legend = "Job characteristic. Hidden job names: "
                            + string.Join(", ", summary.BenchmarksCases.Select(b => b.Job.ResolvedId).Distinct());
                        columnFilters[legendColumnIndex].AddLegend(legend);
                    }
                }
            }
            return columnFilters.Where(cf => !cf.IsHidden).Select(cf => cf.Column);
        }

        private struct CharacteristicFilter
        {
            public readonly Characteristic Characteristic;
            public readonly IColumn? Column;
            public bool Hide;

            public CharacteristicFilter(Characteristic characteristic, IColumn? column)
            {
                Characteristic = characteristic;
                Column = column;
                Hide = false;
            }
        }

        private struct ColumnFilter
        {
            public Characteristic Characteristic { get; }
            public IColumn Column { get; private set; }
            private readonly bool _hide; // per user-defined filter
            private readonly bool _available;
            private readonly bool _multiValue;
            // ***BE CAREFUL*** IsVisible is *NOT* the same as !IsHidden
            // - A column the user wants to hide may not be available in the current context, thus it is neither.
            //   Currently JobCharacteristic.IsAvailable() always returns true, but that can change in the future.
            //   For example, see PR https://github.com/dotnet/BenchmarkDotNet/pull/1621
            // - A column the user wants to hide may contain the same value for all benchmarks and therefore
            //   gets extracted by BDN and the value appears only once above the summary. In this case, too,
            //   the column is neither hidden nor visible for our purposes.
            public bool IsHidden => _available && _multiValue && _hide;
            public bool IsVisible => _available && _multiValue && !_hide;

            public ColumnFilter(CharacteristicFilter characteristicFilter, Summary summary) : this()
            {
                Characteristic = characteristicFilter.Characteristic;
                IColumn column = Column = characteristicFilter.Column ?? throw new ArgumentNullException(nameof(Column));
                _hide = characteristicFilter.Hide;
                if ((_available = Column.IsAvailable(summary)) && summary != null)
                {
                    _multiValue = summary.BenchmarksCases.Select(b => column.GetValue(summary, b)).Distinct().Count() > 1;
                }
            }

            public void AddLegend(string legend)
            {
                if (Column is JobCharacteristicColumnWithLegend)
                    throw new InvalidOperationException("Column already has a legend.");
                Column = new JobCharacteristicColumnWithLegend(Column, legend);
            }
        }

        private class FilterFactory
        {
            private readonly (Characteristic characteristic, IColumn? column)[] _allCharacteristicsAndColumns;
            private readonly Dictionary<string, (int index, int[] childIndices)> _idToIndexLookup;

            public FilterFactory()
            {
                IReadOnlyList<Characteristic> allCharacteristics = CharacteristicHelper.GetAllCharacteristics(typeof(Job));
                _allCharacteristicsAndColumns = new (Characteristic characteristic, IColumn? column)[allCharacteristics.Count];
                _idToIndexLookup = new(_allCharacteristicsAndColumns.Length + 10, StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < allCharacteristics.Count; index++)
                {
                    Characteristic characteristic = allCharacteristics[index];
                    _allCharacteristicsAndColumns[index].characteristic = characteristic;
                    if (characteristic.HasChildCharacteristics)
                    {
                        string type = characteristic.CharacteristicType.Name;
                        int[] childIndices = allCharacteristics.Select((c, i) => c.DeclaringType.Name == type ? i : -1)
                            .Where(i => i != -1).ToArray();
                        _idToIndexLookup.Add(characteristic.Id, (index, childIndices));
                        if (!string.Equals(type, characteristic.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            _idToIndexLookup.Add(type, (index, childIndices)); // Alias
                        }
                    }
                    else
                    {
                        _idToIndexLookup.Add(characteristic.Id, (index, Array.Empty<int>()));
                    }
                }
                _idToIndexLookup.Add("Job", _idToIndexLookup["Id"]); // Alias
                _idToIndexLookup.Add("All", (-1, Enumerable.Range(0, allCharacteristics.Count).ToArray()));
                IColumn[] allColumns = JobCharacteristicColumn.AllColumns;
                for (int i = 0; i < allColumns.Length; i++)
                {
                    _allCharacteristicsAndColumns[_idToIndexLookup[allColumns[i].ColumnName].index].column = allColumns[i];
                }
            }

            public CharacteristicFilter[] CreateCharacteristicFilters(string filterExpression)
            {
                if (filterExpression == null)
                    throw new ArgumentNullException(nameof(filterExpression));
                string[] filterExpressions = filterExpression.Split(new[]{ ' ' }, StringSplitOptions.RemoveEmptyEntries);
                CharacteristicFilter[] retVal =
                    _allCharacteristicsAndColumns.Select(i => new CharacteristicFilter(i.characteristic, i.column)).ToArray();
                for (int i = 0; i < filterExpressions.Length; i++)
                {
                    string filterId = filterExpressions[i];
                    bool hide = filterId[0] == '-';
                    if (filterId[0] is '-' or '+')
                    {
                        filterId = filterId.Substring(1);
                    }
                    ApplyFilter(filterId, hide);
                }
                return retVal;

                void ApplyFilter(string filterId, bool hide)
                {
                    (int index, int[] childIndices) = _idToIndexLookup[filterId];
                    if (index >= 0)
                    {
                        retVal[index].Hide = hide;
                    }
                    for (int i = 0; i < childIndices.Length; i++)
                    {
                        retVal[childIndices[i]].Hide = hide;
                        Characteristic c = retVal[childIndices[i]].Characteristic;
                        if (c.HasChildCharacteristics)
                        {
                            ApplyFilter(c.Id, hide);
                        }
                    }
                }
            }
        }
    }
}
