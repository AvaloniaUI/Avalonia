using System.Diagnostics;
using Avalonia.Input.TextInput;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Window = Tizen.NUI.Window;

namespace Avalonia.Tizen;

internal class NuiAvaloniaViewTextEditable
{
    private INuiTextInput TextInput => _multiline ? _multiLineTextInput : _singleLineTextInput;
    private INuiTextInput _singleLineTextInput;
    private INuiTextInput _multiLineTextInput;
    private bool _breakTheAvaloniaLoop;
    private bool _breakTheTizenLoop;
    private bool _keyboardPresented;
    private bool _multiline = false;

    private TextInputMethodClient _client;
    private NuiAvaloniaView _avaloniaView;

    public bool IsActive => _client != null;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    public NuiAvaloniaViewTextEditable(NuiAvaloniaView avaloniaView)
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    {
        _avaloniaView = avaloniaView;
        _singleLineTextInput = new NuiSingleLineTextInput
        {
            HeightResizePolicy = ResizePolicyType.Fixed,
            WidthResizePolicy = ResizePolicyType.Fixed,
            Size = new(1, 1),
            Position = new Position(-1000, -1000),
            FontSizeScale = 0.1f,
        };

        _multiLineTextInput = new NuiMultiLineTextInput
        {
            HeightResizePolicy = ResizePolicyType.Fixed,
            WidthResizePolicy = ResizePolicyType.Fixed,
            Size = new(1, 1),
            Position = new Position(-1000, -1000),
            FontSizeScale = 0.1f,
        };

        SetupTextInput(_singleLineTextInput);
        SetupTextInput(_multiLineTextInput);
    }

    private void SetupTextInput(INuiTextInput input)
    {
        input.TextChanged += OnTextChanged;
        input.SelectionChanged += OnSelectionChanged;
        input.CursorPositionChanged += OnCursorPositionChanged;
        input.Hide();

        input.GetInputMethodContext().StatusChanged += OnStatusChanged;
    }

    private void OnTextChanged(object? sender, EventArgs e) => InvokeTizenUpdate(() =>
    {
        _avaloniaView.TopLevelImpl.TextInput(TextInput.Text);
    });

    private void OnSelectionChanged(object? sender, EventArgs e) => InvokeTizenUpdate(() =>
    {
        if (_client.Selection.Start == TextInput.SelectedTextStart
            && _client.Selection.End == TextInput.SelectedTextEnd)
            return;

        _client.Selection = new TextSelection(TextInput.SelectedTextStart, TextInput.SelectedTextEnd);
    });

    private void OnCursorPositionChanged(object? sender, EventArgs e) => InvokeTizenUpdate(() =>
    {
        if (_client.Selection.Start == TextInput.PrimaryCursorPosition
            && _client.Selection.End == TextInput.PrimaryCursorPosition)
            return;

        _client.Selection = new TextSelection(TextInput.PrimaryCursorPosition, TextInput.PrimaryCursorPosition);
    });

    private void OnStatusChanged(object? sender, InputMethodContext.StatusChangedEventArgs e)
    {
        _keyboardPresented = e.StatusChanged;
    }

    internal void SetClient(TextInputMethodClient? client)
    {
        if (client == null || !_keyboardPresented)
            DettachAndHide();

        if (client != null)
            AttachAndShow(client);
    }

    internal void SetOptions(TextInputOptions options)
    {
        //TODO: This should be revert when Avalonia used Multiline property
        _multiline = true;
        //if (_multiline != options.Multiline)
        //{
        //    DettachAndHide();
        //    _multiline = options.Multiline;
        //}

        TextInput.Sensitive = options.IsSensitive;
    }

    private void AttachAndShow(TextInputMethodClient client)
    {
        _breakTheTizenLoop = true;
        _breakTheAvaloniaLoop = true;
        try
        {
            TextInput.Text = client.SurroundingText;
            TextInput.PrimaryCursorPosition = client.Selection.Start;
            Window.Instance.GetDefaultLayer().Add((View)TextInput);
            TextInput.Show();
            TextInput.EnableSelection = true;

            var inputContext = TextInput.GetInputMethodContext();
            inputContext.Activate();
            inputContext.ShowInputPanel();
            inputContext.RestoreAfterFocusLost();

            _client = client;
            client.TextViewVisualChanged += OnTextViewVisualChanged;
            client.SurroundingTextChanged += OnSurroundingTextChanged;
            client.SelectionChanged += OnClientSelectionChanged;
        }
        finally
        {
            _breakTheTizenLoop = false;
            _breakTheAvaloniaLoop = false;
        }
    }

    private void OnClientSelectionChanged(object? sender, EventArgs e) => InvokeAvaloniaUpdate(() =>
    {
        if (_client.Selection.End == 0 || _client.Selection.Start == _client.Selection.End)
            TextInput.PrimaryCursorPosition = _client.Selection.Start;
        else
            TextInput.SelectText(_client.Selection.Start, _client.Selection.End);
    });

    private void OnSurroundingTextChanged(object? sender, EventArgs e) => InvokeAvaloniaUpdate(() =>
    {
        TextInput.GetInputMethodContext().SetSurroundingText(_client.SurroundingText);
    });

    private void OnTextViewVisualChanged(object? sender, EventArgs e) => InvokeAvaloniaUpdate(() =>
    {
        TextInput.Text = _client.SurroundingText;
    });

    private void DettachAndHide()
    {
        if (IsActive)
        {
            _client.TextViewVisualChanged -= OnTextViewVisualChanged;
            _client.SurroundingTextChanged -= OnSurroundingTextChanged;
            _client.SelectionChanged += OnClientSelectionChanged;
        }

        if (Window.Instance.GetDefaultLayer().Children.Contains((View)TextInput))
            Window.Instance.GetDefaultLayer().Remove((View)TextInput);

        TextInput.Hide();

        var inputContext = TextInput.GetInputMethodContext();
        inputContext.Deactivate();
        inputContext.HideInputPanel();
    }

    private void InvokeTizenUpdate(Action action)
    {
        if (_breakTheTizenLoop && !IsActive)
            return;

        _breakTheAvaloniaLoop = true;
        try
        {
            action();
        }
        finally { _breakTheAvaloniaLoop = false; }
    }

    private void InvokeAvaloniaUpdate(Action action)
    {
        if (_breakTheAvaloniaLoop && !IsActive)
            return;

        _breakTheTizenLoop = true;
        try
        {
            action();
        }
        finally { _breakTheTizenLoop = false; }
    }
}


internal interface INuiTextInput
{
    string Text { get; set; }
    int PrimaryCursorPosition { get; set; }
    bool EnableSelection { get; set; }
    bool Sensitive { get; set; }
    int SelectedTextStart { get; }
    int SelectedTextEnd { get; }

    event EventHandler TextChanged;
    event EventHandler SelectionChanged;
    event EventHandler CursorPositionChanged;

    void Show();
    InputMethodContext GetInputMethodContext();
    void Hide();
    void SelectText(int selectedTextStart, int value);
}

public class NuiMultiLineTextInput : TextEditor, INuiTextInput
{
    private event EventHandler? _textChanged;

    public NuiMultiLineTextInput()
    {
        base.TextChanged += OnTextChanged;
    }

    event EventHandler INuiTextInput.TextChanged
    {
        add => _textChanged += value;
        remove => _textChanged -= value;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _textChanged?.Invoke(this, EventArgs.Empty);
    }
}

public class NuiSingleLineTextInput : TextField, INuiTextInput
{
    private event EventHandler? _textChanged;

    public NuiSingleLineTextInput()
    {
        base.TextChanged += OnTextChanged;
    }

    event EventHandler INuiTextInput.TextChanged
    {
        add => _textChanged += value;
        remove => _textChanged -= value;
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        _textChanged?.Invoke(this, EventArgs.Empty);
    }
}

//internal class NuiAvaloniaViewTextEditable : ITextEditable
//{
//    private INuiTextInput TextInput => _multiline ? _multiLineTextInput : _singleLineTextInput;
//    private INuiTextInput _singleLineTextInput;
//    private INuiTextInput _multiLineTextInput;
//    private bool _breakTheAvaloniaLoop;
//    private bool _breakTheTizenLoop;
//    private bool _keyboardPresented;
//    private bool _multiline = false;

//    public NuiAvaloniaViewTextEditable()
//    {
//        _singleLineTextInput = new NuiSingleLineTextInput
//        {
//            HeightResizePolicy = ResizePolicyType.Fixed,
//            WidthResizePolicy = ResizePolicyType.Fixed,
//            Size = new(1, 1),
//            Position = new Position(-1000, -1000),
//            FontSizeScale = 0.1f,
//        };

//        _multiLineTextInput = new NuiMultiLineTextInput
//        {
//            HeightResizePolicy = ResizePolicyType.Fixed,
//            WidthResizePolicy = ResizePolicyType.Fixed,
//            Size = new(1, 1),
//            Position = new Position(-1000, -1000),
//            FontSizeScale = 0.1f,
//        };

//        SetupTextInput(_singleLineTextInput);
//        SetupTextInput(_multiLineTextInput);
//    }

//    private void SetupTextInput(INuiTextInput input)
//    {
//        input.TextChanged += OnTextChanged;
//        input.SelectionChanged += OnSelectionChanged;
//        input.CursorPositionChanged += OnCursorPositionChanged;
//        input.Hide();

//        input.GetInputMethodContext().StatusChanged += OnStatusChanged;
//    }

//    private void OnStatusChanged(object? sender, InputMethodContext.StatusChangedEventArgs e) =>
//        _keyboardPresented = e.StatusChanged;

//    public void SetClient(TextInputMethodClient? client)
//    {
//        if (client == null || !_keyboardPresented)
//            DettachAndHide();

//        if (client != null)
//            AttachAndShow(client);
//    }

//    private void AttachAndShow(TextInputMethodClient client)
//    {
//        _breakTheTizenLoop = true;
//        _breakTheAvaloniaLoop = true;
//        try
//        {
//            TextInput.Text = client.SurroundingText.Text;
//            TextInput.PrimaryCursorPosition = client.SurroundingText.CursorOffset;
//            Window.Instance.GetDefaultLayer().Add((View)TextInput);
//            TextInput.Show();
//            TextInput.EnableSelection = true;

//            var inputContext = TextInput.GetInputMethodContext();
//            inputContext.Activate();
//            inputContext.ShowInputPanel();
//            inputContext.RestoreAfterFocusLost();

//            client. = this;
//        }
//        finally
//        {
//            _breakTheTizenLoop = false;
//            _breakTheAvaloniaLoop = false;
//        }
//    }

//    private void DettachAndHide()
//    {
//        if (Window.Instance.GetDefaultLayer().Children.Contains((View)TextInput))
//            Window.Instance.GetDefaultLayer().Remove((View)TextInput);
//        TextInput.Hide();

//        var inputContext = TextInput.GetInputMethodContext();
//        inputContext.Deactivate();
//        inputContext.HideInputPanel();
//    }

//    public void SetOptions(TextInputOptions options)
//    {
//        TextInput.Sensitive = options.IsSensitive;

//        //TODO: This should be revert when Avalonia used Multiline property
//        _multiline = true;
//        //if (_multiline != options.Multiline)
//        //{
//        //    DettachAndHide();
//        //    _multiline = options.Multiline;
//        //}
//    }

//    private void InvokeTizenUpdate(Action action)
//    {
//        if (_breakTheTizenLoop)
//            return;

//        _breakTheAvaloniaLoop = true;
//        try
//        {
//            action();
//        }
//        finally { _breakTheAvaloniaLoop = false; }
//    }

//    private void InvokeAvaloniaUpdate(Action action)
//    {
//        if (_breakTheAvaloniaLoop)
//            return;

//        _breakTheTizenLoop = true;
//        try
//        {
//            action();
//        }
//        finally { _breakTheTizenLoop = false; }
//    }

//    private void OnSelectionChanged(object? sender, EventArgs e) =>
//        InvokeTizenUpdate(() => SelectionChanged?.Invoke(this, EventArgs.Empty));

//    private void OnCursorPositionChanged(object? sender, EventArgs e) =>
//        InvokeTizenUpdate(() => SelectionChanged?.Invoke(this, EventArgs.Empty));

//    private void OnTextChanged(object? sender, EventArgs e) => InvokeTizenUpdate(() =>
//    {
//        TextChanged?.Invoke(this, EventArgs.Empty);
//        SelectionChanged?.Invoke(this, EventArgs.Empty);
//    });

//    public int SelectionStart
//    {
//        get => TextInput.SelectedTextStart;
//        set => InvokeAvaloniaUpdate(() => TextInput.PrimaryCursorPosition = value);
//    }

//    public int SelectionEnd
//    {
//        get => TextInput.SelectedTextEnd;
//        set => InvokeAvaloniaUpdate(() => TextInput.SelectText(TextInput.SelectedTextStart, value));
//    }

//    public int CompositionStart => -1;

//    public int CompositionEnd => -1;

//    public string? Text
//    {
//        get => TextInput.Text;
//        set => InvokeAvaloniaUpdate(() => TextInput.Text = value);
//    }

//    public event EventHandler? TextChanged;
//    public event EventHandler? SelectionChanged;
//    public event EventHandler? CompositionChanged;
//}
