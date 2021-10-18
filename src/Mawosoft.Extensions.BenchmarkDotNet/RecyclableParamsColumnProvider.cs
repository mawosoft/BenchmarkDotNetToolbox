// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;

namespace Mawosoft.Extensions.BenchmarkDotNet
{
    /// <summary>
    /// An alternative to <see cref="DefaultColumnProviders.Params"/> that displays parameters in recyclable
    /// columns corresponding to parameter position rather than name.
    /// </summary>
    public class RecyclableParamsColumnProvider : IColumnProvider
    {
        private readonly bool _tryKeepParamName;
        private readonly string _genericName;

        /// <summary>
        /// Initializes a new instance of the <see cref="RecyclableParamsColumnProvider"/> class.
        /// </summary>
        public RecyclableParamsColumnProvider(bool tryKeepParamName = true, string genericName = "Param")
        {
            _tryKeepParamName = tryKeepParamName;
            _genericName = genericName;
        }

        /// <summary><see cref="IColumnProvider"/> implementation.</summary>
        public IEnumerable<IColumn> GetColumns(Summary summary)
        {
            if (summary == null || summary.BenchmarksCases.Length == 0)
            {
                return Array.Empty<IColumn>();
            }
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
