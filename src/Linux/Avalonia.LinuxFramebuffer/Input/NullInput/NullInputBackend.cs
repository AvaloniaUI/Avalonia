using System;
using Avalonia.Input;
using Avalonia.Input.Raw;

namespace Avalonia.LinuxFramebuffer.Input.NullInput;

internal class NullInputBackend : IInputBackend
{

    public void Initialize(IScreenInfoProvider screen, Action<RawInputEventArgs> onInput)
    {
    }

    public void SetInputRoot(IInputRoot root)
    {
    }
}
