using System.Linq;
using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.Shapes;
using global::Avalonia.Input;
using global::Avalonia.Interactivity;
using global::Avalonia.Markup.Xaml;
using global::Avalonia.Markup.Xaml.MarkupExtensions;
using global::Avalonia.Media;
using global::Avalonia.Platform.Storage;

namespace WinUIEmbedSample;

public partial class EmbeddedView : UserControl
{
    private int _clicks;
    private int _pointCount;
    private int _intermediateCount;

    public EmbeddedView()
    {
        InitializeComponent();
        AvSlider.PropertyChanged += OnSliderPropertyChanged;
        HoverPanel.PointerEntered += (_, _) => HoverState.Text = "over";
        HoverPanel.PointerExited += (_, _) => HoverState.Text = "out";
        AddHandler(KeyDownEvent, OnKeyReadout, handledEventsToo: true);

        DragDrop.SetAllowDrop(DropTargetBorder, true);
        DropTargetBorder.AddHandler(DragDrop.DragOverEvent, OnDropDragOver);
        DropTargetBorder.AddHandler(DragDrop.DropEvent, OnDrop);

        DragSourceBorder.PointerPressed += OnDragSourcePointerPressed;
    }

    private async void OnClipboardCopy(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            ClipboardStatus.Text = "no clipboard";
            return;
        }
        var data = new global::Avalonia.Input.DataTransfer();
        data.Add(DataTransferItem.CreateText(ClipboardText.Text ?? ""));
        await clipboard.SetDataAsync(data);
        ClipboardStatus.Text = $"copied {(ClipboardText.Text ?? "").Length} chars";
    }

    private async void OnClipboardPaste(object? sender, RoutedEventArgs e)
    {
        var clipboard = TopLevel.GetTopLevel(this)?.Clipboard;
        if (clipboard is null)
        {
            ClipboardStatus.Text = "no clipboard";
            return;
        }
        using var data = await clipboard.TryGetDataAsync();
        var text = data is null ? null : await data.TryGetTextAsync();
        if (text is not null)
        {
            ClipboardText.Text = text;
            ClipboardStatus.Text = $"pasted {text.Length} chars";
        }
        else
        {
            ClipboardStatus.Text = "no text on clipboard";
        }
    }

    private async void OnDragSourcePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var data = new global::Avalonia.Input.DataTransfer();
        data.Add(DataTransferItem.CreateText("Hello from Avalonia"));
        try
        {
            await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Copy | DragDropEffects.Move);
        }
        catch
        {
            // Ignore — drag may be cancelled or the source unavailable.
        }
    }

    private void OnDropDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = e.DragEffects & (DragDropEffects.Copy | DragDropEffects.Link | DragDropEffects.Move);
        if (e.DragEffects == DragDropEffects.None)
            e.DragEffects = DragDropEffects.Copy;
        e.Handled = true;
    }

    private void OnDrop(object? sender, DragEventArgs e)
    {
        var dt = e.DataTransfer;
        var files = dt.TryGetValues(DataFormat.File)?.ToArray() ?? System.Array.Empty<IStorageItem>();
        var text = dt.TryGetValue(DataFormat.Text);

        if (files.Length > 0)
            DropStatus.Text = $"{files.Length} file(s):\n" +
                string.Join('\n', files.Select(f => f.Path.LocalPath));
        else if (!string.IsNullOrEmpty(text))
            DropStatus.Text = $"text: {text}";
        else
            DropStatus.Text = "(unknown payload)";

        e.Handled = true;
    }

    private void OnKeyReadout(object? sender, KeyEventArgs e)
    {
        var symbol = e.KeySymbol is { Length: > 0 } s ? $"\"{s}\"" : "null";
        KeyReadout.Text =
            $"Key={e.Key}  Physical={e.PhysicalKey}  Symbol={symbol}\n" +
            $"Modifiers={e.KeyModifiers}";
    }

    private void OnAvButtonClick(object? sender, RoutedEventArgs e)
    {
        AvClickCount.Text = $"Clicked {++_clicks} times";
    }

    private void OnSliderPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == Slider.ValueProperty)
            AvSliderValue.Text = $"Slider: {AvSlider.Value:F0}";
    }

    private void OnInkPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        AddDot(e.GetPosition(InkCanvas), Brushes.Red);
        _pointCount++;
        e.Pointer.Capture(InkCanvas);
        UpdateStats();
        e.Handled = true;
    }

    private void OnInkPointerMoved(object? sender, PointerEventArgs e)
    {
        var intermediates = e.GetIntermediatePoints(InkCanvas);
        for (var i = 0; i < intermediates.Count; i++)
            AddDot(intermediates[i].Position, Brushes.Orange);
        _intermediateCount += intermediates.Count;

        AddDot(e.GetPosition(InkCanvas), Brushes.Green);
        _pointCount++;
        UpdateStats();
    }

    private void OnInkPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        AddDot(e.GetPosition(InkCanvas), Brushes.Blue);
        _pointCount++;
        UpdateStats();
        e.Handled = true;
    }

    private void AddDot(Point position, IBrush brush)
    {
        const double size = 6;
        var dot = new Ellipse
        {
            Width = size,
            Height = size,
            Fill = brush,
        };
        Canvas.SetLeft(dot, position.X - size / 2);
        Canvas.SetTop(dot, position.Y - size / 2);
        InkCanvas.Children.Add(dot);
    }

    private void OnClearInk(object? sender, RoutedEventArgs e)
    {
        InkCanvas.Children.Clear();
        _pointCount = 0;
        _intermediateCount = 0;
        UpdateStats();
    }

    private void UpdateStats()
    {
        InkStats.Text = $"{_pointCount} points / {_intermediateCount} intermediate";
    }
}
