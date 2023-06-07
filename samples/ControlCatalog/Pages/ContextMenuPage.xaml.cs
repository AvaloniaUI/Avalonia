using System;
using System.ComponentModel;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ContextMenuPage : UserControl
    {
        public ContextMenuPage()
        {
            this.InitializeComponent();
            DataContext = new ContextPageViewModel();

            var customContextRequestedBorder = this.Get<Border>("CustomContextRequestedBorder");
            customContextRequestedBorder.AddHandler(ContextRequestedEvent, CustomContextRequested, RoutingStrategies.Tunnel);

            var cancellableContextBorder = this.Get<Border>("CancellableContextBorder");
            cancellableContextBorder.ContextMenu!.Closing += ContextFlyoutPage_Closing;
            cancellableContextBorder.ContextMenu!.Opening += ContextFlyoutPage_Opening;
        }

        private ContextPageViewModel? _model;
        protected override void OnDataContextChanged(EventArgs e)
        {
            if (_model != null)
                _model.View = null;
            _model  = DataContext as ContextPageViewModel;
            if (_model != null)
                _model.View = this;

            base.OnDataContextChanged(e);
        }

        private void ContextFlyoutPage_Closing(object? sender, CancelEventArgs e)
        {
            var cancelCloseCheckBox = this.FindControl<CheckBox>("CancelCloseCheckBox");
            e.Cancel = cancelCloseCheckBox?.IsChecked ?? false;
        }

        private void ContextFlyoutPage_Opening(object? sender, EventArgs e)
        {
            if (e is CancelEventArgs cancelArgs)
            {
                var cancelCloseCheckBox = this.FindControl<CheckBox>("CancelOpenCheckBox");
                cancelArgs.Cancel = cancelCloseCheckBox?.IsChecked ?? false;
            }
        }

        public void CustomContextRequested(object? sender, ContextRequestedEventArgs e)
        {
            if (sender is Border border && border.Child is TextBlock textBlock)
            {
                textBlock.Text = e.TryGetPosition(border, out var point)
                    ? $"Context was requested with pointer at: {point.X:N0}, {point.Y:N0}"
                    : "Context was requested without pointer";
                e.Handled = true;
            }

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
