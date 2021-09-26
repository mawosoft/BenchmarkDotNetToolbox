// Copyright (c) 2021 Matthias Wolf, Mawosoft.

using System;
using BenchmarkDotNet.Parameters;

namespace Mawosoft.BenchmarkDotNetToolbox
{
    /// <summary>
    /// A generic wrapper to associate strongly typed parameter or argument values with a display text.
    /// </summary>
    /// <typeparam name="T">The type of the wrapped value.</typeparam>
    public class ParamWrapper<T> : IDisposable
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

        /// <summary>Disposes the wrapped value if it is disposable.</summary>
        public void Dispose() => (Value as IDisposable)?.Dispose();
    }
}
