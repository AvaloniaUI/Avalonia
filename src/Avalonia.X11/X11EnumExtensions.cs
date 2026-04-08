using Avalonia.Input;

namespace Avalonia.X11;

internal static class X11EnumExtensions
{
    public static RawInputModifiers ToRawInputModifiers(this XModifierMask state)
    {
        var rv = default(RawInputModifiers);
        if (state.HasAllFlags(XModifierMask.Button1Mask))
            rv |= RawInputModifiers.LeftMouseButton;
        if (state.HasAllFlags(XModifierMask.Button2Mask))
            rv |= RawInputModifiers.RightMouseButton;
        if (state.HasAllFlags(XModifierMask.Button3Mask))
            rv |= RawInputModifiers.MiddleMouseButton;
        if (state.HasAllFlags(XModifierMask.Button4Mask))
            rv |= RawInputModifiers.XButton1MouseButton;
        if (state.HasAllFlags(XModifierMask.Button5Mask))
            rv |= RawInputModifiers.XButton2MouseButton;
        if (state.HasAllFlags(XModifierMask.ShiftMask))
            rv |= RawInputModifiers.Shift;
        if (state.HasAllFlags(XModifierMask.ControlMask))
            rv |= RawInputModifiers.Control;
        if (state.HasAllFlags(XModifierMask.Mod1Mask))
            rv |= RawInputModifiers.Alt;
        if (state.HasAllFlags(XModifierMask.Mod4Mask))
            rv |= RawInputModifiers.Meta;
        return rv;
    }
}
