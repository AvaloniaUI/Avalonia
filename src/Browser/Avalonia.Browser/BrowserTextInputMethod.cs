using System;
using System.Runtime.InteropServices.JavaScript;
using Avalonia.Browser.Interop;
using Avalonia.Input.TextInput;
using Avalonia.Input;

namespace Avalonia.Browser;

internal class BrowserTextInputMethod(
    BrowserInputHandler inputHandler,
    JSObject containerElement,
    JSObject inputElement)
    : ITextInputMethodImpl
{
    private JSObject _inputElement = inputElement ?? throw new ArgumentNullException(nameof(inputElement));
    private readonly JSObject _containerElement = containerElement ?? throw new ArgumentNullException(nameof(containerElement));
    private readonly BrowserInputHandler _inputHandler = inputHandler ?? throw new ArgumentNullException(nameof(inputHandler));
    private TextInputMethodClient? _client;
    private bool _multiline;
    private TextInputMethodClient? _compositionClient;
    private IInputElement? _compositionTarget;
    private string? _compositionText;

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
            _client.SelectionChanged -= SurroundingTextChanged;
            _client.InputPaneActivationRequested -= InputPaneActivationRequested;
        }

        if (client != null)
        {
            client.SurroundingTextChanged += SurroundingTextChanged;
            client.SelectionChanged += SurroundingTextChanged;
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
        if (_multiline != options.Multiline)
        {
            var previous = _inputElement;
            _inputElement = InputHelper.SetMultiline(previous, options.Multiline);
            previous.Dispose();
            _multiline = options.Multiline;
        }
    }

    public void Reset()
    {
        InputHelper.ClearInputElement(_inputElement);
        InputHelper.SetSurroundingText(_inputElement, "", 0, 0);
    }

    public void OnBeforeInput(string inputType, int start, int end, string? data, ulong timestamp)
    {
        if (_client is not null && !IsComposing &&
            (inputType == "insertText" || inputType == "insertLineBreak"))
        {
            if (inputType == "insertLineBreak")
            {
                if (!_multiline || data is null)
                    return;
                data = "\n";
            }
            if (data is not null)
            {
                if (start >= 0 && end >= 0)
                    _client.Selection = new TextSelection(start, end);
                _inputHandler.RawTextEvent(timestamp, data);
            }
            return;
        }

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

        if (_compositionTarget is InputElement previous)
            previous.PropertyChanged -= CompositionTargetPropertyChanged;
        _compositionClient = _client;
        _compositionTarget = BrowserWindowingPlatform.Keyboard.FocusedElement;
        _compositionText = null;
        if (_compositionTarget is InputElement target)
            target.PropertyChanged += CompositionTargetPropertyChanged;
        _client.SetPreeditText(null);
        IsComposing = true;
    }

    public void OnCompositionUpdate(string? data)
    {
        _compositionText = data;
        _compositionClient?.SetPreeditText(data);
    }

    private void CompositionTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (args.Property == InputElement.IsFocusedProperty && sender is InputElement { IsFocused: false } && IsComposing)
        {
            // Finish native composition before TextBox clears its selection on blur.
            // Some IMEs deliver compositionend synchronously from blur; the fallback
            // covers clients whose native editor does not produce an end event.
            InputHelper.BlurElement(_inputElement);
            if (IsComposing)
            {
                // Retire the old native editor before another client can compose.
                // A delayed end from it must not end the new client's composition.
                var previous = _inputElement;
                _inputElement = InputHelper.ReplaceInputElement(previous);
                previous.Dispose();
                OnCompositionEnd(0, _compositionText);
            }
        }
    }

    public void OnCompositionEnd(ulong timestamp, string? data)
    {
        // The keyboard focus may have changed before the native DOM blur ends
        // composition. Commit to its original target, never the newly focused field.
        var client = _compositionClient;
        var target = _compositionTarget;
        if (target is InputElement input)
            input.PropertyChanged -= CompositionTargetPropertyChanged;
        _compositionClient = null;
        _compositionTarget = null;
        _compositionText = null;
        IsComposing = false;
        client?.SetPreeditText(null);

        if (data != null && target is Visual { IsAttachedToVisualTree: true } && target.IsEffectivelyEnabled)
        {
            if (ReferenceEquals(target, BrowserWindowingPlatform.Keyboard.FocusedElement))
                _inputHandler.RawTextEvent(timestamp, data);
            else
                target.RaiseEvent(new TextInputEventArgs
                {
                    Text = data,
                    Source = target,
                    RoutedEvent = InputElement.TextInputEvent
                });
        }
    }
}
