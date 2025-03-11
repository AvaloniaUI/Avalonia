using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class InputHelper
{
    public static Task RedirectInputAsync(int topLevelId, Action<BrowserTopLevelImpl> handler)
    {
        if (BrowserTopLevelImpl.TryGetTopLevel(topLevelId) is { } topLevelImpl) handler(topLevelImpl);
        return Task.CompletedTask;
    }

    public static Task<T> RedirectInputRetunAsync<T>(int topLevelId, Func<BrowserTopLevelImpl,T> handler, T @default)
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
    public static Task OnBeforeInput(int topLevelId, string inputType, int start, int end) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.TextInputMethod.OnBeforeInput(inputType, start, end));

    [JSExport]
    public static Task OnCompositionStart(int topLevelId) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.TextInputMethod.OnCompositionStart());

    [JSExport]
    public static Task OnCompositionUpdate(int topLevelId, string? data) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.TextInputMethod.OnCompositionUpdate(data));

    [JSExport]
    public static Task OnCompositionEnd(int topLevelId, string? data) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.TextInputMethod.OnCompositionEnd(data));

    [JSExport]
    public static Task OnPointerMove(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier, JSObject argsObj) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler
            .OnPointerMove(pointerType, pointerId, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier, argsObj));

    [JSExport]
    public static Task OnPointerDown(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId, int buttons,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler
            .OnPointerDown(pointerType, pointerId, buttons, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier));

    [JSExport]
    public static Task OnPointerUp(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId, int buttons,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler
            .OnPointerUp(pointerType, pointerId, buttons, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier));

    [JSExport]
    public static Task OnPointerCancel(int topLevelId, string pointerType, [JSMarshalAs<JSType.Number>] long pointerId,
        double offsetX, double offsetY, double pressure, double tiltX, double tiltY, double twist, int modifier) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler
            .OnPointerCancel(pointerType, pointerId, offsetX, offsetY, pressure, tiltX, tiltY, twist, modifier));

    [JSExport]
    public static Task OnWheel(int topLevelId,
        double offsetX, double offsetY,
        double deltaX, double deltaY, int modifier) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.OnWheel(offsetX, offsetY, deltaX, deltaY, modifier));

    [JSExport]
    public static Task OnDragDrop(int topLevelId, string type, double offsetX, double offsetY, int modifiers, string? effectAllowedStr, JSObject? dataTransfer) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.OnDragEvent(type, offsetX, offsetY, modifiers, effectAllowedStr, dataTransfer));

    [JSExport]
    public static Task OnKeyboardGeometryChange(int topLevelId, double x, double y, double width, double height) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.InputPane
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

    [JSImport("InputHelper.readClipboardText", AvaloniaModule.MainModuleName)]
    public static partial Task<string> ReadClipboardTextAsync(JSObject globalThis);

    [JSImport("InputHelper.writeClipboardText", AvaloniaModule.MainModuleName)]
    public static partial Task WriteClipboardTextAsync(JSObject globalThis, string text);

    [JSImport("InputHelper.setPointerCapture", AvaloniaModule.MainModuleName)]
    public static partial void
        SetPointerCapture(JSObject containerElement, [JSMarshalAs<JSType.Number>] long pointerId);

    [JSImport("InputHelper.releasePointerCapture", AvaloniaModule.MainModuleName)]
    public static partial void ReleasePointerCapture(JSObject containerElement,
        [JSMarshalAs<JSType.Number>] long pointerId);
}
