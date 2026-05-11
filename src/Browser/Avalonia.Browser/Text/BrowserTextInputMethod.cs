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
    private readonly JSObject _inputElement = inputElement ?? throw new ArgumentNullException(nameof(inputElement));
    private readonly JSObject _containerElement = containerElement ?? throw new ArgumentNullException(nameof(containerElement));
    private readonly BrowserInputHandler _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
    private TextInputMethodClient? _client;

    private static readonly bool s_supportsEditContext = InputHelper.SupportsEditContext();

    private IInputMethod _inputMethod = s_supportsEditContext ? new EditContextInputMethod(inputElement, topLevelId) : new ClassicInputMethod(inputElement);

    public bool IsComposing { get; private set; }

    private void HideIme()
    {
        InputHelper.HideElement(inputElement);
        InputHelper.FocusElement(_containerElement);
    }

    public void SetClient(TextInputMethodClient? client)
    {
        if (_client != null)
        {
            _client.SurroundingTextChanged -= SurroundingTextChanged;
            _client.InputPaneActivationRequested -= InputPaneActivationRequested;
            _client.SelectionChanged -= Client_SelectionChanged;
        }

        if (client != null)
        {
            client.SurroundingTextChanged += SurroundingTextChanged;
            client.InputPaneActivationRequested += InputPaneActivationRequested;
            client.SelectionChanged += Client_SelectionChanged;
        }

        _inputMethod.SetClient(client);
        _inputMethod.ClearInput();

        _client = client;

        if (_client != null)
        {
            ShowIme();

            var surroundingText = _client.SurroundingText ?? "";
            var selection = _client.Selection;

            _inputMethod.SetSurroundingText(surroundingText, selection.Start, selection.End);
        }
        else
        {
            HideIme();
        }
    }

    private void Client_SelectionChanged(object? sender, EventArgs e)
    {
        if (_client != null && _inputMethod is EditContextInputMethod inputMethod)
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
        if (_client != null)
        {
            var surroundingText = _client.SurroundingText ?? "";
            var selection = _client.Selection;

            _inputMethod.SetSurroundingText(surroundingText, selection.Start, selection.End);
        }
    }

    public void SetCursorRect(Rect rect)
    {
        _inputMethod.SetCursorRect(rect);
    }

    public void SetOptions(TextInputOptions options)
    {
        _inputMethod.SetOptions(options);
    }

    public void Reset()
    {
        _inputMethod.Reset();
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

    internal void OnTextUpdate(int rangeStart, int rangeEnd, string? text, int selectionStart, int selectionEnd)
    {
        if (_client != null && _inputMethod is EditContextInputMethod inputMethod)
        {
            inputMethod.IsUpdating = true;
            try
            {
                _client.Selection = new TextSelection(rangeStart, rangeEnd);
                var isDelete = string.IsNullOrEmpty(text);

                if (!isDelete && rangeStart != rangeEnd)
                {
                    _inputHandler.RawTextEvent(text ?? "");
                }
                else
                {
                    inputHandler.OnKeyDown("Delete", "Delete", 0);
                    inputHandler.OnKeyUp("Delete", "Delete", 0);
                }
            }
            finally
            {
                inputMethod.IsUpdating = false;
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
}
