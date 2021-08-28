// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    public class RecyclableParamsColumnProvider : IColumnProvider
    {
        private readonly bool _tryKeepParamName;
        private readonly string _genericName;
        public RecyclableParamsColumnProvider(bool tryKeepParamName = true, string genericName = "Param")
        {
            _tryKeepParamName = tryKeepParamName;
            _genericName = genericName;
        }

        public IEnumerable<IColumn> GetColumns(Summary summary)
        {
            int maxParamCount = summary.BenchmarksCases.Max(b => b.Parameters.Count);
            List<IColumn> columns = new(maxParamCount);
            for (int paramIndex = 0; paramIndex < maxParamCount; paramIndex++)
            {
                // netstandard2.0 compat. Otherwise we would use ToHashSet() instead of Distinct().ToList()
                List<string> names = summary.BenchmarksCases.Where(b => b.Parameters.Count > paramIndex).Select(b => b.Parameters.Items.ElementAt(paramIndex).Definition.Name).Distinct().ToList();
                Debug.Assert(names.Count > 0);
                if (names.Count > 0)
                {
                    bool isRealName = _tryKeepParamName && names.Count == 1;
                    columns.Add(new RecyclableParamColumn(paramIndex, isRealName ? names.First() : _genericName + (paramIndex + 1), isRealName));
                }
            }
            return columns;
        }
    }
}
