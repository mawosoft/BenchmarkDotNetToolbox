// Copyright (c) 2021-2023 Matthias Wolf, Mawosoft.

namespace Mawosoft.Extensions.BenchmarkDotNet;

/// <summary>
/// A generic wrapper to associate strongly typed parameter or argument values with a display text.
/// </summary>
[SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "Existing API.")]
[SuppressMessage("Design", "CA1063:Implement IDisposable Correctly", Justification = "Wrapper only.")]
[SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize", Justification = "Wrapper only.")]
public class ParamWrapper<T> : IDisposable
{
    /// <summary>
    /// The strongly typed parameter or argument value.
    /// </summary>
    public T Value;

    /// <summary>
    /// The associated text to display in logs and summaries.
    /// </summary>
    public string? DisplayText;

    /// <summary>
    /// Returns a string that represents the wrapped value.
    /// </summary>
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

    /// <summary>
    /// Disposes the wrapped value if it implements <see cref="IDisposable"/>.
    /// </summary>
    public void Dispose() => (Value as IDisposable)?.Dispose();
}
