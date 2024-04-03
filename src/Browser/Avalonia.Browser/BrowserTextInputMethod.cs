using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input.TextInput;

namespace Avalonia.Browser;

internal class BrowserTextInputMethod : ITextInputMethodImpl
{
    private readonly JSObject _inputElement;
    private readonly JSObject _containerElement;
    private readonly BrowserInputHandler _inputHandler;
    private TextInputMethodClient? _client;

    public BrowserTextInputMethod(BrowserInputHandler inputHandler, JSObject containerElement, JSObject inputElement)
    {
        _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
        _containerElement = containerElement ?? throw new ArgumentNullException(nameof(containerElement));
        _inputElement = inputElement ?? throw new ArgumentNullException(nameof(inputElement));

        InputHelper.SubscribeTextEvents(
            _inputElement,
            OnBeforeInput,
            OnCompositionStart,
            OnCompositionUpdate,
            OnCompositionEnd);
    }

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
        }

        if (client != null)
        {
            client.SurroundingTextChanged += SurroundingTextChanged;
        }

        InputHelper.ClearInputElement(_inputElement);

        _client = client;

        if (_client != null)
        {
            InputHelper.ShowElement(_inputElement);
            InputHelper.FocusElement(_inputElement);

            var surroundingText = _client.SurroundingText ?? "";
            var selection = _client.Selection;

            InputHelper.SetSurroundingText(_inputElement, surroundingText, selection.Start, selection.End);
        }
        else
        {
            HideIme();
        }
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

    private bool OnBeforeInput(JSObject arg, int start, int end)
    {
        var type = arg.GetPropertyAsString("inputType");
        if (type != "deleteByComposition")
        {
            if (type == "deleteContentBackward")
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

        return false;
    }

    private bool OnCompositionStart(JSObject args)
    {
        if (_client == null)
            return false;

        _client.SetPreeditText(null);
        IsComposing = true;

        return false;
    }

    private bool OnCompositionUpdate(JSObject args)
    {
        if (_client == null)
            return false;

        _client.SetPreeditText(args.GetPropertyAsString("data"));

        return false;
    }

    private bool OnCompositionEnd(JSObject args)
    {
        if (_client == null)
            return false;

        IsComposing = false;

        _client.SetPreeditText(null);

        var text = args.GetPropertyAsString("data");

        if (text != null)
        {
            return _inputHandler.RawTextEvent(text);
        }

        return false;
    }
}
