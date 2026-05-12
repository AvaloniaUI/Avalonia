using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input.TextInput;

namespace Avalonia.Browser.Text;

internal class BrowserTextInputMethod(
    BrowserInputHandler inputHandler,
    JSObject containerElement,
    JSObject inputElement,
    int topLevelId)
    : ITextInputMethodImpl
{
    private readonly JSObject _containerElement = containerElement ?? throw new ArgumentNullException(nameof(containerElement));
    private readonly BrowserInputHandler _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
    private TextInputMethodClient? _client;

    private readonly EditContextInputMethod? _inputMethod = InputHelper.SupportsEditContext() ? new(inputElement, topLevelId) : null;

    public bool IsComposing { get; private set; }

    private void HideIme()
    {
        InputHelper.HideElement(inputElement);
        InputHelper.FocusElement(_containerElement);
    }

    public void SetClient(TextInputMethodClient? client)
    {
        _client?.InputPaneActivationRequested -= InputPaneActivationRequested;
        if (_inputMethod != null)
        {
            _client?.SurroundingTextChanged -= SurroundingTextChanged;
            _client?.SelectionChanged -= Client_SelectionChanged;

            client?.SurroundingTextChanged += SurroundingTextChanged;
            client?.SelectionChanged += Client_SelectionChanged;

            _inputMethod.SetClient(client);
            _inputMethod.ClearInput();
        }

        _client = client;
        client?.InputPaneActivationRequested += InputPaneActivationRequested;

        if (_client != null)
        {
            ShowIme();

            if (_inputMethod != null)
            {
                var surroundingText = _client.SurroundingText ?? "";
                var selection = _client.Selection;

                _inputMethod.SetSurroundingText(surroundingText, selection.Start, selection.End);
            }
        }
        else
        {
            HideIme();
        }
    }

    private void Client_SelectionChanged(object? sender, EventArgs e)
    {
        if (_client != null && _inputMethod != null)
        {
            var surroundingText = _client.SurroundingText ?? "";
            var selection = _client.Selection;

            _inputMethod.SetSurroundingText(surroundingText, selection.Start, selection.End);
        }
    }

    private void InputPaneActivationRequested(object? sender, EventArgs e)
    {
        if (_client != null)
        {
            ShowIme();
        }
    }

    private void ShowIme()
    {
        InputHelper.ShowElement(inputElement);
        InputHelper.FocusElement(inputElement);
    }

    private void SurroundingTextChanged(object? sender, EventArgs e)
    {
        if (_client != null && _inputMethod != null)
        {
            var surroundingText = _client.SurroundingText ?? "";
            var selection = _client.Selection;

            _inputMethod.SetSurroundingText(surroundingText, selection.Start, selection.End);
        }
    }

    public void SetCursorRect(Rect rect)
    {
        _inputMethod?.SetCursorRect(rect);
    }

    public void SetOptions(TextInputOptions options)
    {
        _inputMethod?.SetOptions(options);
    }

    public void Reset()
    {
        _inputMethod?.Reset();
    }

    internal void OnTextUpdate(int rangeStart, int rangeEnd, string? text, int selectionStart, int selectionEnd)
    {
        if (_client != null && _inputMethod != null)
        {
            if (_inputMethod.IsUpdating)
                return;

            _inputMethod.IsUpdating = true;
            try
            {
                _client.Selection = new TextSelection(rangeStart, rangeEnd);
                var isDelete = string.IsNullOrEmpty(text);

                if (!isDelete)
                {
                    _inputHandler.RawTextEvent(text ?? "");
                }
                else if (rangeStart != rangeEnd)
                {
                    inputHandler.OnKeyDown("Delete", "Delete", 0);
                    inputHandler.OnKeyUp("Delete", "Delete", 0);
                }
            }
            finally
            {
                _inputMethod.IsUpdating = false;
                _client.Selection = new TextSelection(selectionStart, selectionEnd);
            }
        }
    }

    internal void OnCharacterBoundsUpdate(int rangeStart, int rangeEnd)
    {
        if (_inputMethod is EditContextInputMethod editContextInputMethod)
        {
            editContextInputMethod.UpdateCharacterBounds();
        }
    }

    internal void OnCompositionEnd()
    {
        if (_client == null)
            return;

        IsComposing = false;
    }

    internal void OnCompositionStart()
    {
        if (_client == null)
            return;

        IsComposing = true;
    }
}
