// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using BenchmarkDotNet.Parameters;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    public class ParamWrapper<T>
    {
        public T Value;
        public string? DisplayText;
        public override string? ToString() => DisplayText ?? Value?.ToString() ?? ParameterInstance.NullParameterTextRepresentation;
        public ParamWrapper(T value, string? displayText)
        {
            Value = value;
            DisplayText = displayText;
        }
    }
}
