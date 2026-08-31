using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class InputHelper
{
    public static void RedirectInput(int topLevelId, Action<BrowserTopLevelImpl> handler)
    {
        if (BrowserTopLevelImpl.TryGetTopLevel(topLevelId) is { } topLevelImpl)
            handler(topLevelImpl);
    }

    public static Task<T> RedirectInputRetunAsync<T>(int topLevelId, Func<BrowserTopLevelImpl, T> handler, T @default)
    {
        if (BrowserTopLevelImpl.TryGetTopLevel(topLevelId) is { } topLevelImpl)
            return Task.FromResult(handler(topLevelImpl));
        return Task.FromResult(@default);
    }

    [JSImport("InputHelper.subscribeInputEvents", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeInputEvents(JSObject htmlElement, int topLevelId);

    [JSExport]
    public static Task<bool> OnKeyDown(int topLevelId, string code, string key, int modifier) =>
        RedirectInputRetunAsync(topLevelId, t => t.InputHandler.OnKeyDown(code, key, modifier), false);

    [JSExport]
    public static Task<bool> OnKeyUp(int topLevelId, string code, string key, int modifier) =>
        RedirectInputRetunAsync(topLevelId, t => t.InputHandler.OnKeyUp(code, key, modifier), false);

    [JSExport]
    public static void OnBeforeInput(int topLevelId, string inputType, int start, int end) =>
        RedirectInput(topLevelId, t => t.InputHandler.TextInputMethod.OnBeforeInput(inputType, start, end));

    [JSExport]
    public static void OnCompositionStart(int topLevelId) =>
        RedirectInput(topLevelId, t => t.InputHandler.TextInputMethod.OnCompositionStart());

    [JSExport]
    public static void OnCompositionUpdate(int topLevelId, string? data) =>
        RedirectInput(topLevelId, t => t.InputHandler.TextInputMethod.OnCompositionUpdate(data));

    [JSExport]
    public static void OnCompositionEnd(int topLevelId, string? data) =>
        RedirectInput(topLevelId, t => t.InputHandler.TextInputMethod.OnCompositionEnd(data));

    [JSExport]
    public static void OnPointerMove(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier, JSObject argsObj) =>
        RedirectInput(topLevelId, t => t.InputHandler
            .OnPointerMove(pointerType, pointerId, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier, argsObj));

    [JSExport]
    public static void OnPointerDown(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId, int buttons,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier) =>
        RedirectInput(topLevelId, t => t.InputHandler
            .OnPointerDown(pointerType, pointerId, buttons, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier));

    [JSExport]
    public static void OnPointerUp(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId, int buttons,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier) =>
        RedirectInput(topLevelId, t => t.InputHandler
            .OnPointerUp(pointerType, pointerId, buttons, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier));

    [JSExport]
    public static void OnPointerCancel(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier) =>
        RedirectInput(topLevelId, t => t.InputHandler
            .OnPointerCancel(pointerType, pointerId, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier));

    [JSExport]
    public static void OnWheel(int topLevelId,
        double offsetX, double offsetY,
        double deltaX, double deltaY, int modifier) =>
        RedirectInput(topLevelId, t => t.InputHandler.OnWheel(offsetX, offsetY, deltaX, deltaY, modifier));

    [JSExport]
    public static void OnDragDrop(int topLevelId, string type, double offsetX, double offsetY, int modifiers, JSObject dataTransfer, JSObject items) =>
        RedirectInput(topLevelId, t => t.InputHandler.OnDragEvent(type, offsetX, offsetY, modifiers, dataTransfer, items));

    [JSExport]
    public static void OnKeyboardGeometryChange(int topLevelId, double x, double y, double width, double height) =>
        RedirectInput(topLevelId, t => t.InputHandler.InputPane
            .OnGeometryChange(x, y, width, height));

    [JSImport("InputHelper.getCoalescedEvents", AvaloniaModule.MainModuleName)]
    [return: JSMarshalAs<JSType.Array<JSType.Number>>]
    public static partial double[] GetCoalescedEvents(JSObject pointerEvent);

    [JSImport("InputHelper.clearInput", AvaloniaModule.MainModuleName)]
    public static partial void ClearInputElement(JSObject htmlElement);

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
    public static partial void InitializeBackgroundHandlers(JSObject globalThis);

    [JSImport("InputHelper.isClipboardFormatSupported", AvaloniaModule.MainModuleName)]
    public static partial bool IsClipboardFormatSupported(string format);

    [JSImport("InputHelper.createWriteableClipboardSource", AvaloniaModule.MainModuleName)]
    public static partial JSObject CreateWriteableClipboardSource();

    [JSImport("InputHelper.createWriteableClipboardItem", AvaloniaModule.MainModuleName)]
    public static partial JSObject CreateWriteableClipboardItem(JSObject source);

    [JSImport("InputHelper.addStringToWriteableClipboardItem", AvaloniaModule.MainModuleName)]
    public static partial void AddStringToWriteableClipboardItem(JSObject item, string format, string value);

    [JSImport("InputHelper.addBytesToWriteableClipboardItem", AvaloniaModule.MainModuleName)]
    public static partial void AddBytesToWriteableClipboardItem(JSObject item, string format, [JSMarshalAs<JSType.MemoryView>] Span<byte> value);

    [JSImport("InputHelper.readClipboard", AvaloniaModule.MainModuleName)]
    public static partial Task<JSObject> ReadClipboardAsync(JSObject window);

    [JSImport("InputHelper.writeClipboard", AvaloniaModule.MainModuleName)]
    public static partial Task<string> WriteClipboardAsync(JSObject globalThis, JSObject? source);

    [JSImport("InputHelper.getReadableDataItemFormats", AvaloniaModule.MainModuleName)]
    public static partial string[] GetReadableDataItemFormats(JSObject item);

    [JSImport("InputHelper.tryGetReadableDataItemValueAsync", AvaloniaModule.MainModuleName)]
    public static partial Task<JSObject?> TryGetReadableDataItemValueAsync(JSObject item, string format);

    [JSImport("InputHelper.tryGetReadableDataItemValue", AvaloniaModule.MainModuleName)]
    public static partial JSObject? TryGetReadableDataItemValue(JSObject item, string format);

    [JSImport("InputHelper.setPointerCapture", AvaloniaModule.MainModuleName)]
    public static partial void
        SetPointerCapture(JSObject containerElement, [JSMarshalAs<JSType.Number>] long pointerId);

    [JSImport("InputHelper.releasePointerCapture", AvaloniaModule.MainModuleName)]
    public static partial void ReleasePointerCapture(JSObject containerElement,
        [JSMarshalAs<JSType.Number>] long pointerId);
}
