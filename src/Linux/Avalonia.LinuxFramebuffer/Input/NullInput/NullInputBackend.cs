using System;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.LinuxFramebuffer.Input.NullInput;
/// <summary>
/// Null Input Backend
/// </summary>
public class NullInputBackend : IInputBackend
{
    /// <inheritdoc />
    public void Initialize(IScreenInfoProvider screen, Action<RawInputEventArgs> onInput)
    {
    }

    /// <inheritdoc />
    public void SetInputRoot(IInputRoot root)
    {
    }
}
