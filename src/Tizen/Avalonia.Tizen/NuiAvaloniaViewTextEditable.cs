using System.Diagnostics;
using Avalonia.Input.TextInput;
using Tizen.NUI;
using Tizen.NUI.BaseComponents;
using Window = Tizen.NUI.Window;

namespace Avalonia.Tizen;

internal class NuiAvaloniaViewTextEditable : ITextEditable
{
    private TextField _textField;
    private bool _breakTheAvaloniaLoop;
    private bool _breakTheTizenLoop;

    public NuiAvaloniaViewTextEditable()
    {
        _textField = new TextField
        {
            HeightResizePolicy = ResizePolicyType.Fixed,
            WidthResizePolicy = ResizePolicyType.Fixed,
            Size = new(1, 1),
            Position = new Position(-1000, -1000),
            FontSizeScale = 0.1f, 
        };

        _textField.TextChanged += OnTextChanged;
        _textField.SelectionChanged += OnSelectionChanged;

        _textField.CursorPositionChanged += OnCursorPositionChanged;

        _textField.Hide();
    }

    public void SetClient(ITextInputMethodClient? client)
    {
        if (client == null)
            DettachAndHide();
        else
            AttachAndShow(client);
    }

    private void AttachAndShow(ITextInputMethodClient client)
    {
        _textField.Text = client.SurroundingText.Text;
        _textField.PrimaryCursorPosition = client.SurroundingText.CursorOffset;
        Window.Instance.GetDefaultLayer().Add(_textField);
        _textField.Show();
        _textField.EnableSelection = true;

        var inputContext = _textField.GetInputMethodContext();
        inputContext.Activate();
        inputContext.ShowInputPanel();
        inputContext.RestoreAfterFocusLost();

        client.TextEditable = this;
    }

    private void DettachAndHide()
    {
        Window.Instance.GetDefaultLayer().Remove(_textField);
        _textField.Hide();

        var inputContext = _textField.GetInputMethodContext();
        inputContext.Deactivate();
        inputContext.HideInputPanel();
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

    private void OnTextChanged(object? sender, TextField.TextChangedEventArgs e) => InvokeTizenUpdate(() =>
    {
        TextChanged?.Invoke(this, EventArgs.Empty);
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    });

    public int SelectionStart
    {
        get => _textField.SelectedTextStart;
        set => InvokeAvaloniaUpdate(() => _textField.PrimaryCursorPosition = value);
    }

    public int SelectionEnd
    {
        get => _textField.SelectedTextEnd;
        set => InvokeAvaloniaUpdate(() => _textField.SelectText(_textField.SelectedTextStart, value));
    }

    public int CompositionStart => -1;

    public int CompositionEnd => -1;

    public string? Text
    {
        get => _textField.Text;
        set => InvokeAvaloniaUpdate(() => _textField.Text = value);
    }

    public event EventHandler? TextChanged;
    public event EventHandler? SelectionChanged;
    public event EventHandler? CompositionChanged;
}
