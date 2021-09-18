// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>
    /// An alternative to <see cref="DefaultColumnProviders.Params"/> that displays params in recyclable
    /// columns corresponding to param position rather than param name.
    /// </summary>
    public class RecyclableParamsColumnProvider : IColumnProvider
    {
        private readonly bool _tryKeepParamName;
        private readonly string _genericName;
        /// <summary>
        /// Initializes a new instance of the <see cref="RecyclableParamsColumnProvider"/> class.
        /// </summary>
        /// <param name="tryKeepParamName">
        /// If true and if all params at a position have the same name, that name will be used as column
        /// header. Otherwise, a generic, numbered column header will be used.
        /// </param>
        /// <param name="genericName">Prefix for the generic, numbered column header.</param>
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
                HashSet<string> names = new(summary.BenchmarksCases
                    .Where(b => b.Parameters.Count > paramIndex)
                    .Select(b => b.Parameters.Items.ElementAt(paramIndex).Definition.Name));
                Debug.Assert(names.Count > 0);
                if (names.Count > 0)
                {
                    bool isRealName = _tryKeepParamName && names.Count == 1;
                    columns.Add(new RecyclableParamColumn(paramIndex, isRealName
                        ? names.First()
                        : _genericName + (paramIndex + 1), isRealName));
                }
            }
            return columns;
        }
    }
}
