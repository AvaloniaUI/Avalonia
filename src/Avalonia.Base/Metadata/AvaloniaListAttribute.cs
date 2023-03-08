using System;

namespace Avalonia.Metadata;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class AvaloniaListAttribute : Attribute
{
    public string[]? Separators { get; init; }

    // StringSplitOptions.TrimEntries = 2, but only on net6 target.
    public StringSplitOptions SplitOptions { get; init; } = StringSplitOptions.RemoveEmptyEntries | (StringSplitOptions)2;
}
