using System;

namespace Avalonia.Metadata;

/// <summary>
/// Defines how compiler should split avalonia list string value before parsing individual items.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AvaloniaListAttribute : Attribute
{
    /// <summary>
    /// Separator used to split input string.
    /// Default value is ','.
    /// </summary>
    public string[]? Separators { get; init; }

    /// <summary>
    /// Split options used to split input string.
    /// Default value is RemoveEmptyEntries with TrimEntries.
    /// </summary>
    // StringSplitOptions.TrimEntries = 2, but only on net6 target.
    public StringSplitOptions SplitOptions { get; init; } = StringSplitOptions.RemoveEmptyEntries | (StringSplitOptions)2;
}
