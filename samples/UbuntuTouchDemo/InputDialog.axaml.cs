using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Platform;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace UbuntuTouchDemo;

public sealed partial class InputDialog : Window
{
    public InputDialog()
    {
        InitializeComponent();
    }

    public async Task ShowForOwnerAsync(Window owner)
    {
        var preferredWidth = Width;
        // The owner's input context is deactivated while its modal child has focus.
        // Retain the initial keyboard boundary when positioning the dialog.
        var keyboardTop = owner.InputPane is { State: InputPaneState.Open } pane
            ? (double?)pane.OccludedRect.Top : null;
        var ownerSize = owner.ClientSize;
        var closed = false;

        void FitDialog()
        {
            if (closed)
                return;
            var scaling = owner.RenderScaling;
            var origin = owner.PointToScreen(default);
            var area = new PixelRect(origin, PixelSize.FromSize(owner.ClientSize, scaling));
            if (owner.Screens.ScreenFromWindow(owner) is { } screen)
                area = area.Intersect(screen.WorkingArea);
            if (keyboardTop is { } top && owner.ClientSize == ownerSize)
                area = area.WithHeight(Math.Min(area.Height,
                    Math.Max(0, origin.Y + (int)(top * scaling) - area.Y)));
            var margin = (int)Math.Ceiling(16 * scaling);
            MaxWidth = Math.Max(1, area.Width - 2 * margin) / scaling;
            MaxHeight = Math.Max(1, area.Height - 2 * margin) / scaling;
            Width = Math.Min(preferredWidth, MaxWidth);
            if (IsVisible)
            {
                var size = PixelSize.FromSize(FrameSize ?? ClientSize, DesktopScaling);
                Position = new PixelPoint(area.X + Math.Max(0, (area.Width - size.Width) / 2),
                    area.Y + Math.Max(0, (area.Height - size.Height) / 2));
            }
        }

        void Refit(object? sender, EventArgs args)
            => Dispatcher.UIThread.Post(FitDialog, DispatcherPriority.Loaded);
        void OnDialogOpened(object? sender, EventArgs args)
        {
            FitDialog();
            InputField.Focus();
        }

        FitDialog();
        Opened += OnDialogOpened;
        Resized += Refit;
        owner.Resized += Refit;
        owner.Screens.Changed += Refit;
        try { await ShowDialog(owner); }
        finally
        {
            closed = true;
            Opened -= OnDialogOpened;
            Resized -= Refit;
            owner.Resized -= Refit;
            owner.Screens.Changed -= Refit;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
