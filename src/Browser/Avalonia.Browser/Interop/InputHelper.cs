using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Avalonia.Browser.Interop;

internal static partial class InputHelper
{
    public static Task RedirectInputAsync(int topLevelId, Action<BrowserTopLevelImpl> handler)
    {
        if (BrowserTopLevelImpl.TryGetTopLevel(topLevelId) is { } topLevelImpl)
            handler(topLevelImpl);
        return Task.CompletedTask;
    }

    [JSImport("InputHelper.subscribeInputEvents", AvaloniaModule.MainModuleName)]
    public static partial void SubscribeInputEvents(JSObject htmlElement, int topLevelId);

    /// <summary>
    /// Hands the ring buffer control block to JS. <paramref name="threadingEnabled"/> tells JS that
    /// synchronous exports cannot be called from the browser main thread.
    /// </summary>
    [JSImport("InputQueue.attach", AvaloniaModule.MainModuleName)]
    public static partial void AttachInputQueue(int controlBlockPtr, bool threadingEnabled);

    /// <summary>
    /// Asks JS to move spilled records back into the (empty) ring. Returns the number of records moved.
    /// </summary>
    [JSImport("InputQueue.spillOverflow", AvaloniaModule.MainModuleName)]
    public static partial int SpillOverflow();

    /// <summary>
    /// Called once per burst of queued input, from a JS macrotask. Single-threaded runtime only.
    /// </summary>
    [JSExport]
    public static void OnInputWake()
    {
        if (BrowserWindowingPlatform.DispatcherImpl is { } dispatcherImpl)
            dispatcherImpl.RunInputWake(BrowserInputQueue.Drain);
        else
            BrowserInputQueue.Drain(); // managed dispatcher: the UI thread pumps the grouper queue itself
    }

    /// <summary>
    /// Same as <see cref="OnInputWake"/>, for runtimes where the main thread can only make asynchronous calls.
    /// </summary>
    [JSExport]
    public static Task OnInputWakeAsync()
    {
        OnInputWake();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Drains the ring and dispatches everything inline. Returns the handled state of the last event,
    /// which is the one whose DOM handler is asking. Single-threaded runtime only.
    /// </summary>
    [JSExport]
    public static bool FlushInputSync() => BrowserInputQueue.FlushSync();

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
    public static Task OnDragDrop(int topLevelId, string type, double offsetX, double offsetY, int modifiers, JSObject dataTransfer, JSObject items) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.OnDragEvent(type, offsetX, offsetY, modifiers, dataTransfer, items));

    [JSExport]
    public static Task OnKeyboardGeometryChange(int topLevelId, double x, double y, double width, double height) =>
        RedirectInputAsync(topLevelId, t => t.InputHandler.InputPane
            .OnGeometryChange(x, y, width, height));

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
