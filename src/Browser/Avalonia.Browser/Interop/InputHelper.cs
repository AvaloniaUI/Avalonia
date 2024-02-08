using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class InputHelper
{
    [JSImport("InputHelper.subscribeKeyEvents", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeKeyEvents(
        JSObject htmlElement,
        [JSMarshalAs<JSType.Function<JSType.String, JSType.String, JSType.Number, JSType.Boolean>>]
        Func<string, string, int, bool> keyDown,
        [JSMarshalAs<JSType.Function<JSType.String, JSType.String, JSType.Number, JSType.Boolean>>]
        Func<string, string, int, bool> keyUp);

    [JSImport("InputHelper.subscribeTextEvents", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeTextEvents(
        JSObject htmlElement,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Number, JSType.Number, JSType.Boolean>>]
        Func<JSObject, int, int, bool> onBeforeInput,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> onCompositionStart,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> onCompositionUpdate,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> onCompositionEnd);

    [JSImport("InputHelper.subscribePointerEvents", AvaloniaModule.MainModuleName)]
    public static partial void SubscribePointerEvents(
        JSObject htmlElement,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> pointerMove,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> pointerDown,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> pointerUp,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> pointerCancel,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>]
        Func<JSObject, bool> wheel);

    [JSImport("InputHelper.subscribeInputEvents", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeInputEvents(
        JSObject htmlElement,
        [JSMarshalAs<JSType.Function<JSType.String, JSType.Boolean>>]
        Func<string, bool> input);

    [JSImport("InputHelper.subscribeDropEvents", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeDropEvents(JSObject containerElement,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>] Func<JSObject, bool> dragEvent);

    [JSImport("InputHelper.subscribeKeyboardGeometryChange", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeKeyboardGeometryChange(JSObject containerElement,
        [JSMarshalAs<JSType.Function<JSType.Object, JSType.Boolean>>] Func<JSObject, bool> handler);

    [JSImport("InputHelper.subscribeVisibilityChange", AvaloniaModule.MainModuleName)]
    public static partial bool SubscribeVisibilityChange([JSMarshalAs<JSType.Function<JSType.Boolean>>] Action<bool> handler);

    [JSImport("InputHelper.getCoalescedEvents", AvaloniaModule.MainModuleName)]
    [return: JSMarshalAs<JSType.Array<JSType.Object>>]
    public static partial JSObject[] GetCoalescedEvents(JSObject pointerEvent);

    [JSImport("InputHelper.clearInput", AvaloniaModule.MainModuleName)]
    public static partial void ClearInputElement(JSObject htmlElement);

    [JSImport("InputHelper.isInputElement", AvaloniaModule.MainModuleName)]
    public static partial void IsInputElement(JSObject htmlElement);

    [JSImport("InputHelper.focusElement", AvaloniaModule.MainModuleName)]
    public static partial void FocusElement(JSObject htmlElement);

    [JSImport("InputHelper.setCursor", AvaloniaModule.MainModuleName)]
    public static partial void SetCursor(JSObject htmlElement, string kind);

    [JSImport("InputHelper.hide", AvaloniaModule.MainModuleName)]
    public static partial void HideElement(JSObject htmlElement);

    [JSImport("InputHelper.show", AvaloniaModule.MainModuleName)]
    public static partial void ShowElement(JSObject htmlElement);

    [JSImport("InputHelper.setSurroundingText", AvaloniaModule.MainModuleName)]
    public static partial void SetSurroundingText(JSObject htmlElement, string text, int start, int end);

    [JSImport("InputHelper.setBounds", AvaloniaModule.MainModuleName)]
    public static partial void SetBounds(JSObject htmlElement, int x, int y, int width, int height, int caret);

    [JSImport("InputHelper.initializeBackgroundHandlers", AvaloniaModule.MainModuleName)]
    public static partial void InitializeBackgroundHandlers();

    [JSImport("InputHelper.readClipboardText", AvaloniaModule.MainModuleName)]
    public static partial Task<string> ReadClipboardTextAsync();

    [JSImport("globalThis.navigator.clipboard.writeText")]
    public static partial Task WriteClipboardTextAsync(string text);
}
