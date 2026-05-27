using System;
using System.Runtime.InteropServices;
using global::Avalonia;
using global::Avalonia.Input;
using global::Avalonia.Input.Raw;
using global::Avalonia.Input.TextInput;
using global::Avalonia.Logging;
using Microsoft.UI.Xaml;
using Windows.UI.Text.Core;
using AvRect = global::Avalonia.Rect;

namespace Avalonia.WinUI;

/// <summary>
/// Bridges Avalonia's <see cref="ITextInputMethodImpl"/> contract to the WinRT <see cref="CoreTextEditContext"/>.
/// </summary>
internal sealed class WinUITextInputMethod : ITextInputMethodImpl
{
    private readonly AvaloniaSwapChainPanel _panel;
    private readonly Func<Action<RawInputEventArgs>?> _getInput;
    private readonly Func<IInputRoot?> _getInputRoot;
    private readonly Func<IKeyboardDevice?> _getKeyboardDevice;

    private CoreTextEditContext? _editContext;
    private TextInputMethodClient? _client;
    private bool _hasFocus;
    private bool _isComposing;
    private string _imeText = string.Empty;
    private CoreTextRange _imeSelection;
    private int _compositionStart;
    private int _compositionLength;
    private AvRect _cursorRect;
    private bool _suppressClientEcho;

    public WinUITextInputMethod(
        AvaloniaSwapChainPanel panel,
        Func<Action<RawInputEventArgs>?> getInput,
        Func<IInputRoot?> getInputRoot,
        Func<IKeyboardDevice?> getKeyboardDevice)
    {
        _panel = panel;
        _getInput = getInput;
        _getInputRoot = getInputRoot;
        _getKeyboardDevice = getKeyboardDevice;
    }

    private CoreTextEditContext GetOrCreateContext()
    {
        if (_editContext is not null)
            return _editContext;

        var manager = CoreTextServicesManager.GetForCurrentView();
        var ctx = manager.CreateEditContext();
        ctx.InputPaneDisplayPolicy = CoreTextInputPaneDisplayPolicy.Automatic;
        ctx.InputScope = CoreTextInputScope.Text;

        ctx.TextRequested += OnTextRequested;
        ctx.SelectionRequested += OnSelectionRequested;
        ctx.LayoutRequested += OnLayoutRequested;
        ctx.TextUpdating += OnTextUpdating;
        ctx.SelectionUpdating += OnSelectionUpdating;
        ctx.FormatUpdating += OnFormatUpdating;
        ctx.CompositionStarted += OnCompositionStarted;
        ctx.CompositionCompleted += OnCompositionCompleted;
        ctx.FocusRemoved += OnFocusRemoved;

        _editContext = ctx;
        return ctx;
    }

    public void SetClient(TextInputMethodClient? client)
    {
        if (_client is not null)
        {
            _client.SurroundingTextChanged -= OnClientSurroundingTextChanged;
            _client.SelectionChanged -= OnClientSelectionChanged;
        }

        _client = client;
        ResetCompositionState();

        if (client is null)
        {
            if (_editContext is not null && _hasFocus)
                _editContext.NotifyFocusLeave();
            return;
        }

        client.SurroundingTextChanged += OnClientSurroundingTextChanged;
        client.SelectionChanged += OnClientSelectionChanged;

        var ctx = GetOrCreateContext();
        SyncFromClient(notifyServer: true);
        if (_hasFocus)
            ctx.NotifyFocusEnter();
    }

    public void SetCursorRect(Rect rect)
    {
        _cursorRect = rect;
        _editContext?.NotifyLayoutChanged();
    }

    public void SetOptions(TextInputOptions options)
    {
        if (_editContext is null)
            return;
        _editContext.InputScope = options.ContentType switch
        {
            TextInputContentType.Email => CoreTextInputScope.EmailAddress,
            TextInputContentType.Number => CoreTextInputScope.Number,
            TextInputContentType.Password => CoreTextInputScope.Password,
            TextInputContentType.Digits => CoreTextInputScope.Digits,
            TextInputContentType.Url => CoreTextInputScope.Url,
            TextInputContentType.Search => CoreTextInputScope.Search,
            _ => CoreTextInputScope.Text,
        };
    }

    public void Reset()
    {
        if (_isComposing)
            _client?.SetPreeditText(null);
        ResetCompositionState();
        SyncFromClient(notifyServer: true);
    }

    /// <summary>Called by the panel when it gains/loses focus.</summary>
    public void OnPanelFocusChanged(bool hasFocus)
    {
        _hasFocus = hasFocus;
        if (_editContext is null)
            return;
        if (hasFocus && _client is not null)
            _editContext.NotifyFocusEnter();
        else
            _editContext.NotifyFocusLeave();
    }

    private void ResetCompositionState()
    {
        _isComposing = false;
        _compositionStart = 0;
        _compositionLength = 0;
    }

    private void SyncFromClient(bool notifyServer)
    {
        if (_client is null)
        {
            _imeText = string.Empty;
            _imeSelection = default;
            return;
        }

        _imeText = _client.SurroundingText ?? string.Empty;
        var sel = _client.Selection;
        _imeSelection = new CoreTextRange
        {
            StartCaretPosition = Math.Clamp(sel.Start, 0, _imeText.Length),
            EndCaretPosition = Math.Clamp(sel.End, 0, _imeText.Length),
        };

        if (notifyServer && _editContext is not null)
        {
            _editContext.NotifyTextChanged(
                new CoreTextRange { StartCaretPosition = 0, EndCaretPosition = int.MaxValue },
                _imeText.Length,
                _imeSelection);
            _editContext.NotifyLayoutChanged();
        }
    }

    private void OnClientSurroundingTextChanged(object? sender, EventArgs e)
    {
        if (_suppressClientEcho || _isComposing)
            return;
        SyncFromClient(notifyServer: true);
    }

    private void OnClientSelectionChanged(object? sender, EventArgs e)
    {
        if (_suppressClientEcho || _isComposing || _client is null || _editContext is null)
            return;
        var sel = _client.Selection;
        _imeSelection = new CoreTextRange
        {
            StartCaretPosition = Math.Clamp(sel.Start, 0, _imeText.Length),
            EndCaretPosition = Math.Clamp(sel.End, 0, _imeText.Length),
        };
        _editContext.NotifySelectionChanged(_imeSelection);
    }

    private void OnTextRequested(CoreTextEditContext sender, CoreTextTextRequestedEventArgs args)
    {
        var range = args.Request.Range;
        var start = Math.Clamp(range.StartCaretPosition, 0, _imeText.Length);
        var end = Math.Clamp(range.EndCaretPosition, start, _imeText.Length);
        args.Request.Text = _imeText.Substring(start, end - start);
    }

    private void OnSelectionRequested(CoreTextEditContext sender, CoreTextSelectionRequestedEventArgs args)
    {
        args.Request.Selection = _imeSelection;
    }

    private void OnLayoutRequested(CoreTextEditContext sender, CoreTextLayoutRequestedEventArgs args)
    {
        try
        {
            var screenRect = ComputeScreenCursorRect();
            args.Request.LayoutBounds.TextBounds = screenRect;
            args.Request.LayoutBounds.ControlBounds = screenRect;
        }
        catch (Exception ex)
        {
            // Panel not in tree yet, or transform unavailable — leave bounds default.
            Logger.TryGet(LogEventLevel.Verbose, LogArea.WinUIPlatform)?.Log(this, "LayoutRequested couldn't compute screen rect: {Message}", ex.Message);
        }
    }

    private Windows.Foundation.Rect ComputeScreenCursorRect()
    {
        var transform = _panel.TransformToVisual(null);
        var topLeft = transform.TransformPoint(
            new Windows.Foundation.Point(_cursorRect.X, _cursorRect.Y));
        var width = Math.Max(_cursorRect.Width, 1);
        var height = Math.Max(_cursorRect.Height, 16);

        var xamlRoot = _panel.XamlRoot;
        var scale = xamlRoot?.RasterizationScale ?? 1.0;

        var clientX = topLeft.X * scale;
        var clientY = topLeft.Y * scale;
        var w = width * scale;
        var h = height * scale;

        if (xamlRoot?.ContentIslandEnvironment is { } island)
        {
            var hwnd = Microsoft.UI.Win32Interop.GetWindowFromWindowId(island.AppWindowId);
            if (hwnd != IntPtr.Zero)
            {
                var pt = new POINT { x = 0, y = 0 };
                if (ClientToScreen(hwnd, ref pt))
                {
                    clientX += pt.x;
                    clientY += pt.y;
                }
            }
        }

        return new Windows.Foundation.Rect(clientX, clientY, w, h);
    }

    private void OnTextUpdating(CoreTextEditContext sender, CoreTextTextUpdatingEventArgs args)
    {
        var newText = args.Text ?? string.Empty;
        var range = args.Range;
        var start = Math.Clamp(range.StartCaretPosition, 0, _imeText.Length);
        var end = Math.Clamp(range.EndCaretPosition, start, _imeText.Length);

        _imeText = _imeText.Substring(0, start) + newText + _imeText.Substring(end);
        _imeSelection = args.NewSelection;

        if (_isComposing)
        {
            if (_compositionLength == 0)
                _compositionStart = start;
            _compositionLength = (_compositionStart + _compositionLength + (newText.Length - (end - start))) - _compositionStart;
            _compositionLength = Math.Max(0, _imeSelection.EndCaretPosition - _compositionStart);
            _compositionLength = Math.Min(_compositionLength, _imeText.Length - _compositionStart);

            var preedit = _imeText.Substring(_compositionStart, _compositionLength);
            _client?.SetPreeditText(preedit, _imeSelection.EndCaretPosition - _compositionStart);
        }
        else
        {
            CommitToClient(newText, start, end);
        }

        args.Result = CoreTextTextUpdatingResult.Succeeded;
    }

    private void OnSelectionUpdating(CoreTextEditContext sender, CoreTextSelectionUpdatingEventArgs args)
    {
        _imeSelection = args.Selection;
        if (!_isComposing && _client is not null)
        {
            _suppressClientEcho = true;
            try
            {
                _client.Selection = new TextSelection(
                    _imeSelection.StartCaretPosition, _imeSelection.EndCaretPosition);
            }
            finally { _suppressClientEcho = false; }
        }
        args.Result = CoreTextSelectionUpdatingResult.Succeeded;
    }

    private void OnFormatUpdating(CoreTextEditContext sender, CoreTextFormatUpdatingEventArgs args)
    {
        args.Result = CoreTextFormatUpdatingResult.Succeeded;
    }

    private void OnCompositionStarted(CoreTextEditContext sender, CoreTextCompositionStartedEventArgs args)
    {
        _isComposing = true;
        _compositionStart = _imeSelection.StartCaretPosition;
        _compositionLength = 0;
    }

    private void OnCompositionCompleted(CoreTextEditContext sender, CoreTextCompositionCompletedEventArgs args)
    {
        _isComposing = false;
        var len = Math.Max(0, Math.Min(_compositionLength, _imeText.Length - _compositionStart));
        var committed = len > 0 ? _imeText.Substring(_compositionStart, len) : string.Empty;

        _client?.SetPreeditText(null);
        _compositionLength = 0;

        if (!string.IsNullOrEmpty(committed))
        {
            var input = _getInput();
            var inputRoot = _getInputRoot();
            var keyboard = _getKeyboardDevice();
            if (input is not null && inputRoot is not null && keyboard is not null)
            {
                _suppressClientEcho = true;
                try
                {
                    input(new RawTextInputEventArgs(keyboard, (ulong)Environment.TickCount64, inputRoot, committed));
                }
                finally
                {
                    _suppressClientEcho = false;
                }
            }

            SyncFromClient(notifyServer: false);
        }
    }

    private void OnFocusRemoved(CoreTextEditContext sender, object args)
    {
        _hasFocus = false;
    }

    private void CommitToClient(string newText, int start, int end)
    {
        if (string.IsNullOrEmpty(newText))
            return;
        DispatchRawText(newText);
    }

    private void DispatchRawText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return;
        var input = _getInput();
        var inputRoot = _getInputRoot();
        var keyboard = _getKeyboardDevice();
        if (input is null || inputRoot is null || keyboard is null)
            return;

        _suppressClientEcho = true;
        try
        {
            input(new RawTextInputEventArgs(keyboard, (ulong)Environment.TickCount64, inputRoot, text));
        }
        finally
        {
            _suppressClientEcho = false;
        }

        // The client just changed; pull its new state and propagate to the server.
        SyncFromClient(notifyServer: true);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT lpPoint);
}
