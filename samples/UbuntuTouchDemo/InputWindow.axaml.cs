using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace UbuntuTouchDemo;

public sealed partial class InputWindow : Window
{
    private TextBox? _lastField;
    private IInputPane? _inputPane;

    public InputWindow()
    {
        InitializeComponent();
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        Console.WriteLine($"Window scale={RenderScaling} size={ClientSize} clipboard={Clipboard?.GetType().Name}");
        _inputPane = InputPane;
        if (_inputPane is not null)
            _inputPane.StateChanged += OnInputPaneStateChanged;
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_inputPane is not null)
            _inputPane.StateChanged -= OnInputPaneStateChanged;
        base.OnClosed(e);
    }

    private void OnInputPaneStateChanged(object? sender, InputPaneStateEventArgs e)
    {
        KeyboardStatus.Text = e.NewState == InputPaneState.Open ? "Keyboard open" : "Keyboard closed";
        Console.WriteLine($"InputPane {e.NewState}: {e.EndRect}");
        // Reserve only the measured overlap; a compositor-resized window has none.
        var overlap = e.NewState == InputPaneState.Open && e.EndRect.Bottom >= ClientSize.Height - 1
            ? e.EndRect.Height : 0;
        FormScroll.Margin = Dialog.Margin = new Thickness(0, 0, 0, overlap);
        Dispatcher.UIThread.Post(() =>
        {
            if (Dialog.IsVisible)
                Dialog.BringInputIntoView();
            else
                _lastField?.BringIntoView();
        }, DispatcherPriority.Loaded);
    }

    private void OnFieldGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (sender is TextBox field)
        {
            _lastField = field;
            Console.WriteLine($"Focus: {field.PlaceholderText}");
            Dispatcher.UIThread.Post(field.BringIntoView, DispatcherPriority.Loaded);
        }
    }

    private void OnFieldTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (sender is TextBox field)
            Console.WriteLine($"Edited: {field.PlaceholderText}; content redacted");
    }

    private void OnCopyClick(object? sender, RoutedEventArgs e) => _lastField?.Copy();
    private void OnCutClick(object? sender, RoutedEventArgs e) => _lastField?.Cut();
    private void OnPasteClick(object? sender, RoutedEventArgs e) => _lastField?.Paste();

    private void OnAddFieldClick(object? sender, RoutedEventArgs e)
    {
        var template = (IDataTemplate)this.FindResource("DynamicFieldTemplate")!;
        if (template.Build(null) is TextBox field)
        {
            Form.Children.Add(field);
            field.Focus();
        }
    }

    private void OnDialogClick(object? sender, RoutedEventArgs e)
    {
        FormScroll.IsVisible = false;
        Dialog.IsVisible = true;
        Dispatcher.UIThread.Post(Dialog.FocusInput, DispatcherPriority.Loaded);
    }

    private void OnDialogCloseRequested(object? sender, EventArgs e)
    {
        Dialog.IsVisible = false;
        FormScroll.IsVisible = true;
        Dispatcher.UIThread.Post(() => _lastField?.Focus(), DispatcherPriority.Loaded);
    }
}
