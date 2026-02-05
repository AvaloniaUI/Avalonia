using Avalonia.Input;
using Avalonia.Platform;

namespace Avalonia.Rendering;

/// <summary>
/// This is a temporary bridge interface to bring services from PresentationSource
/// (which currently lives in Avalonia.Controls) to Avalonia.Base.
/// </summary>
internal interface IPresentationSource : IInputRoot
{
    internal IPlatformSettings? PlatformSettings { get; }
}