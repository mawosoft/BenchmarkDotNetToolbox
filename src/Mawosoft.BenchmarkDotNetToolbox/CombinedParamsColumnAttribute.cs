// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using BenchmarkDotNet.Attributes;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    public class CombinedParamsColumnAttribute : ColumnConfigBaseAttribute
    {
        public CombinedParamsColumnAttribute() : base(new CombinedParamsColumn()) { }
        public CombinedParamsColumnAttribute(string formatNameValue = "{0}={1}", string separator = ", ", string prefix = "", string suffix = "") : base(new CombinedParamsColumn(formatNameValue, separator, prefix, suffix)) { }
    }
}
