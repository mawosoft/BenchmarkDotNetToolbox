// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Parameters;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>
    /// A strongly typed wrapper to associate parameter or argument values with a display text.
    /// </summary>
    /// <remarks>
    /// For use with values returned by methods annotated with <see cref="ArgumentsSourceAttribute"/> and
    /// fields or properties annotated with <see cref="ParamsSourceAttribute"/>.
    /// </remarks>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public class ParamWrapper<T>
    {
        /// <summary>The strongly typed parameter or argument value.</summary>
        public T Value;
        /// <summary>The associated text to display in logs and summaries.</summary>
        public string? DisplayText;
        public override string? ToString()
            => DisplayText ?? Value?.ToString() ?? ParameterInstance.NullParameterTextRepresentation;
        /// <summary>
        /// Initializes a new instance of the <see cref="ParamWrapper{T}"/> class with the given strongly
        /// typed value and display text.
        /// </summary>
        public ParamWrapper(T value, string? displayText)
        {
            Value = value;
            DisplayText = displayText;
        }
    }
}
