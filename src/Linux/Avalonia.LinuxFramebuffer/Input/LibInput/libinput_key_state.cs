//Base on from https://github.com/unoplatform/uno/blob/59ee50ec892d9a533acdee81412f7a383d704cf2/src/Uno.UI.Runtime.Skia.Linux.FrameBuffer/Native/libinput_key_state.cs
namespace Avalonia.LinuxFramebuffer.Input.LibInput;

internal enum libinput_key_state
{
    Released = 0,
    Pressed = 1
};
