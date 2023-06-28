using System.Diagnostics;
using Avalonia.Input.TextInput;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Window = Tizen.NUI.Window;

namespace Avalonia.Tizen;

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

internal class NuiAvaloniaViewTextEditable : ITextEditable
{
    private INuiTextInput TextInput => _multiline ? _multiLineTextInput : _singleLineTextInput;
    private INuiTextInput _singleLineTextInput;
    private INuiTextInput _multiLineTextInput;
    private bool _breakTheAvaloniaLoop;
    private bool _breakTheTizenLoop;
    private bool _keyboardPresented;
    private bool _multiline = false;

    public NuiAvaloniaViewTextEditable()
    {
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

    private void OnStatusChanged(object? sender, InputMethodContext.StatusChangedEventArgs e) =>
        _keyboardPresented = e.StatusChanged;

    public void SetClient(ITextInputMethodClient? client)
    {
        if (client == null || !_keyboardPresented)
            DettachAndHide();

        if (client != null)
            AttachAndShow(client);
    }

    private void AttachAndShow(ITextInputMethodClient client)
    {
        _breakTheTizenLoop = true;
        _breakTheAvaloniaLoop = true;
        try
        {
            TextInput.Text = client.SurroundingText.Text;
            TextInput.PrimaryCursorPosition = client.SurroundingText.CursorOffset;
            Window.Instance.GetDefaultLayer().Add((View)TextInput);
            TextInput.Show();
            TextInput.EnableSelection = true;

            var inputContext = TextInput.GetInputMethodContext();
            inputContext.Activate();
            inputContext.ShowInputPanel();
            inputContext.RestoreAfterFocusLost();

            client.TextEditable = this;
        }
        finally
        {
            _breakTheTizenLoop = false;
            _breakTheAvaloniaLoop = false;
        }
    }

    private void DettachAndHide()
    {
        if (Window.Instance.GetDefaultLayer().Children.Contains((View)TextInput))
            Window.Instance.GetDefaultLayer().Remove((View)TextInput);
        TextInput.Hide();

        var inputContext = TextInput.GetInputMethodContext();
        inputContext.Deactivate();
        inputContext.HideInputPanel();
    }

    public void SetOptions(TextInputOptions options)
    {
        TextInput.Sensitive = options.IsSensitive;
        if (_multiline != options.Multiline)
        {
            DettachAndHide();
            _multiline = options.Multiline;
        }
    }

    private void InvokeTizenUpdate(Action action)
    {
        if (_breakTheTizenLoop)
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
        if (_breakTheAvaloniaLoop)
            return;

        _breakTheTizenLoop = true;
        try
        {
            action();
        }
        finally { _breakTheTizenLoop = false; }
    }

    private void OnSelectionChanged(object? sender, EventArgs e) =>
        InvokeTizenUpdate(() => SelectionChanged?.Invoke(this, EventArgs.Empty));

    private void OnCursorPositionChanged(object? sender, EventArgs e) =>
        InvokeTizenUpdate(() => SelectionChanged?.Invoke(this, EventArgs.Empty));

    private void OnTextChanged(object? sender, EventArgs e) => InvokeTizenUpdate(() =>
    {
        TextChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    });

    public int SelectionStart
    {
        get => TextInput.SelectedTextStart;
        set => InvokeAvaloniaUpdate(() => TextInput.PrimaryCursorPosition = value);
    }

    public int SelectionEnd
    {
        get => TextInput.SelectedTextEnd;
        set => InvokeAvaloniaUpdate(() => TextInput.SelectText(TextInput.SelectedTextStart, value));
    }

    public int CompositionStart => -1;

    public int CompositionEnd => -1;

    public string? Text
    {
        get => TextInput.Text;
        set => InvokeAvaloniaUpdate(() => TextInput.Text = value);
    }

    public event EventHandler? TextChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? CompositionChanged;
}
