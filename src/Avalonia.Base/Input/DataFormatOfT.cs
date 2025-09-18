using System.Diagnostics.CodeAnalysis;

namespace Avalonia.Input;

/// <summary>
/// Represents a format usable with the clipboard and drag-and-drop.
/// </summary>
/// <remarks>
/// This class cannot be instantiated directly.
/// Use universal formats such as <see cref="DataFormat.Text"/> and <see cref="DataFormat.File"/>,
/// or create custom formats using <see cref="DataFormat.CreateApplicationFormat"/>
/// or <see cref="DataFormat.CreatePlatformFormat"/>.
/// </remarks>
[SuppressMessage("ReSharper", "UnusedTypeParameter", Justification = "Used to resolve typed overloads.")]
public sealed record DataFormat<T> : DataFormat
    where T : class
{
    internal DataFormat(DataFormatKind kind, string identifier)
        : base(kind, identifier)
    {
    }

    /// <inheritdoc />
    public override string ToString()
        => base.ToString();
}
