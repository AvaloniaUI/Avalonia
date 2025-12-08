using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input.TextInput;

namespace Avalonia.Browser;

internal class BrowserTextInputMethod(
    BrowserInputHandler inputHandler,
    JSObject containerElement,
    JSObject inputElement)
    : ITextInputMethodImpl
{
    private readonly JSObject _inputElement = inputElement ?? throw new ArgumentNullException(nameof(inputElement));
    private readonly JSObject _containerElement = containerElement ?? throw new ArgumentNullException(nameof(containerElement));
    private readonly BrowserInputHandler _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
    private TextInputMethodClient? _client;

    public bool IsComposing { get; private set; }

    private void HideIme()
    {
        InputHelper.HideElement(_inputElement);
        InputHelper.FocusElement(_containerElement);
    }

    public void SetClient(TextInputMethodClient? client)
    {
        if (_client != null)
        {
            _client.SurroundingTextChanged -= SurroundingTextChanged;
            _client.InputPaneActivationRequested -= InputPaneActivationRequested;
        }

        if (client != null)
        {
            client.SurroundingTextChanged += SurroundingTextChanged;
            client.InputPaneActivationRequested += InputPaneActivationRequested;
        }

        InputHelper.ClearInputElement(_inputElement);

        _client = client;

        if (_client != null)
        {
            ShowIme();

            var surroundingText = _client.SurroundingText ?? "";
            var selection = _client.Selection;

            InputHelper.SetSurroundingText(_inputElement, surroundingText, selection.Start, selection.End);
        }
        else
        {
            HideIme();
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
        InputHelper.ShowElement(_inputElement);
        InputHelper.FocusElement(_inputElement);
    }

    private void SurroundingTextChanged(object? sender, EventArgs e)
    {
        if (_client != null)
        {
            var surroundingText = _client.SurroundingText ?? "";
            var selection = _client.Selection;

            InputHelper.SetSurroundingText(_inputElement, surroundingText, selection.Start, selection.End);
        }
    }

    public void SetCursorRect(Rect rect)
    {
        InputHelper.FocusElement(_inputElement);
        InputHelper.SetBounds(_inputElement, (int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height,
            _client?.Selection.End ?? 0);
        InputHelper.FocusElement(_inputElement);
    }

    public void SetOptions(TextInputOptions options)
    {
    }

    public void Reset()
    {
        InputHelper.ClearInputElement(_inputElement);
        InputHelper.SetSurroundingText(_inputElement, "", 0, 0);
    }

    public void OnBeforeInput(string inputType, int start, int end)
    {
        if (inputType != "deleteByComposition")
        {
            if (inputType == "deleteContentBackward")
            {
                start = _inputElement.GetPropertyAsInt32("selectionStart");
                end = _inputElement.GetPropertyAsInt32("selectionEnd");
            }
            else
            {
                start = -1;
                end = -1;
            }
        }

        if (start != -1 && end != -1 && _client != null)
        {
            _client.Selection = new TextSelection(start, end);
        }
    }

    public void OnCompositionStart()
    {
        if (_client == null)
            return;

        _client.SetPreeditText(null);
        IsComposing = true;
    }

    public void OnCompositionUpdate(string? data)
    {
        if (_client == null)
            return;

        _client.SetPreeditText(data);
    }

    public void OnCompositionEnd(string? data)
    {
        if (_client == null)
            return;

        IsComposing = false;

        _client.SetPreeditText(null);
        
        if (data != null)
        {
            _inputHandler.RawTextEvent(data);
        }
    }
}
