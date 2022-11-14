using Avalonia.Input.Raw;

namespace Avalonia.Input
{
    internal static class RawInputHelpers
    {
        public static KeyModifiers ToKeyModifiers(this RawInputModifiers modifiers) =>
            (KeyModifiers)(modifiers & RawInputModifiers.KeyboardMask);

        public static PointerUpdateKind ToUpdateKind(this RawPointerEventType type) => type switch
        {
            RawPointerEventType.LeftButtonDown => PointerUpdateKind.LeftButtonPressed,
            RawPointerEventType.LeftButtonUp => PointerUpdateKind.LeftButtonReleased,
            RawPointerEventType.RightButtonDown => PointerUpdateKind.RightButtonPressed,
            RawPointerEventType.RightButtonUp => PointerUpdateKind.RightButtonReleased,
            RawPointerEventType.MiddleButtonDown => PointerUpdateKind.MiddleButtonPressed,
            RawPointerEventType.MiddleButtonUp => PointerUpdateKind.MiddleButtonReleased,
            RawPointerEventType.XButton1Down => PointerUpdateKind.XButton1Pressed,
            RawPointerEventType.XButton1Up => PointerUpdateKind.XButton1Released,
            RawPointerEventType.XButton2Down => PointerUpdateKind.XButton2Pressed,
            RawPointerEventType.XButton2Up => PointerUpdateKind.XButton2Released,
            RawPointerEventType.TouchBegin => PointerUpdateKind.LeftButtonPressed,
            RawPointerEventType.TouchEnd => PointerUpdateKind.LeftButtonReleased,
            _ => PointerUpdateKind.Other
        };
    }
}
