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
    public class JobColumnSelectionProvider : IColumnProvider
    {
        private static readonly Lazy<FilterFactory> s_filterFactory = new();

        private readonly bool _showHiddenValuesInLegend;
        private readonly CharacteristicFilter[] _characteristicFilters;

        public JobColumnSelectionProvider(bool showHiddenValuesInLegend, params string[] filterExpressions)
        {
            _showHiddenValuesInLegend = showHiddenValuesInLegend;
            _characteristicFilters = s_filterFactory.Value.CreateCharacteristicFilters(filterExpressions);
        }

        public IEnumerable<IColumn> GetColumns(Summary summary)
        {
            ColumnFilter[] columnFilters = _characteristicFilters.Where(c => c.Column != null).Select(c => new ColumnFilter(c)).ToArray();
            IEnumerable<Job> jobs = summary.BenchmarksCases.Select(b => b.Job);
            for (int i = 0; i < columnFilters.Length; i++)
            {
                if (columnFilters[i].Available = columnFilters[i].Column.IsAvailable(summary))
                {
                    columnFilters[i].MultiValue = jobs.Select(j => CharacteristicPresenter.DefaultPresenter.ToPresentation(j, columnFilters[i].Characteristic)).Distinct().Count() > 1;
                }
            }
            if (_showHiddenValuesInLegend && columnFilters.Any(cf => cf.IsHidden))
            {
                int firstVisible = Array.FindIndex(columnFilters, cf => cf.IsVisible);
                if (firstVisible >= 0)
                {
                    // TODO select only the hidden characteristics
                    string legend = "Job characteristics" + Environment.NewLine + string.Join(Environment.NewLine, jobs.Select(j => j.DisplayInfo).Distinct());
                    columnFilters[firstVisible].Column = new JobCharacteristicColumnWithLegend(columnFilters[firstVisible].Column, legend);
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
            // TODO readonly for ctor initialized fields (however, we want to replace one Column int the array)
            public Characteristic Characteristic;
            public IColumn Column;
            public bool Hide;
            public bool Available;
            public bool MultiValue;

            public bool IsHidden => Available && MultiValue && Hide;
            public bool IsVisible => Available && MultiValue && !Hide;

            public ColumnFilter(CharacteristicFilter characteristicFilter) : this()
            {
                Characteristic = characteristicFilter.Characteristic;
                Column = characteristicFilter.Column ?? throw new ArgumentNullException(nameof(Column));
                Hide = characteristicFilter.Hide;
            }
        }

        private class FilterFactory
        {
            private readonly (Characteristic characteristic, IColumn? column)[] _items;
            private readonly Dictionary<string, (int index, int[] childIndices)> _lookup;

            public FilterFactory()
            {
                IReadOnlyList<Characteristic> allCharacteristics = CharacteristicHelper.GetAllCharacteristics(typeof(Job));
                _items = new (Characteristic characteristic, IColumn? column)[allCharacteristics.Count];
                _lookup = new(_items.Length + 10, StringComparer.OrdinalIgnoreCase);
                for (int index = 0; index < allCharacteristics.Count; index++)
                {
                    Characteristic characteristic = allCharacteristics[index];
                    _items[index].characteristic = characteristic;
                    if (characteristic.HasChildCharacteristics)
                    {
                        string type = characteristic.CharacteristicType.Name;
                        int[] childIndices = allCharacteristics.Select((c, i) => c.DeclaringType.Name == type ? i : -1).Where(i => i != -1).ToArray();
                        _lookup.Add(characteristic.Id, (index, childIndices));
                        if (!string.Equals(type, characteristic.Id, StringComparison.OrdinalIgnoreCase))
                        {
                            _lookup.Add(type, (index, childIndices));
                        }
                    }
                    else
                    {
                        _lookup.Add(characteristic.Id, (index, Array.Empty<int>()));
                    }
                }
                _lookup.Add("Job", _lookup["Id"]);
                _lookup.Add("All", (-1, Enumerable.Range(0, allCharacteristics.Count).ToArray()));
                IColumn[] allColumns = JobCharacteristicColumn.AllColumns;
                for (int i = 0; i < allColumns.Length; i++)
                {
                    _items[_lookup[allColumns[i].ColumnName].index].column = allColumns[i];
                }
            }

            public CharacteristicFilter[] CreateCharacteristicFilters(params string[] filterExpressions)
            {
                if (filterExpressions == null)
                    throw new ArgumentNullException(nameof(filterExpressions));
                CharacteristicFilter[] retVal = _items.Select(i => new CharacteristicFilter(i.characteristic, i.column)).ToArray();
                for (int i = 0; i < filterExpressions.Length; i++)
                {
                    string filterExpression = filterExpressions[i];
                    if (string.IsNullOrEmpty(filterExpression)) throw new ArgumentException(null, $"{nameof(filterExpressions)}[{i}]");
                    bool hide = filterExpression[0] == '-';
                    if (filterExpression[0] is '-' or '+')
                    {
                        filterExpression = filterExpression.Substring(1);
                    }
                    ApplyFilter(filterExpression, hide);
                }
                return retVal;

                void ApplyFilter(string filterId, bool hide)
                {
                    (int index, int[] childIndices) = _lookup[filterId];
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
