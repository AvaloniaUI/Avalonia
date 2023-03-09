using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;
using Avalonia.Interactivity;
using System;
using System.ComponentModel;

namespace ControlCatalog.Pages
{
    public class ContextFlyoutPage : UserControl
    {
        private TextBox _textBox;

        public ContextFlyoutPage()
        {
            InitializeComponent();

            DataContext = new ContextPageViewModel();

            _textBox = this.Get<TextBox>("TextBox");

            var cutButton = this.Get<Button>("CutButton");
            cutButton.Click += CloseFlyout;

            var copyButton = this.Get<Button>("CopyButton");
            copyButton.Click += CloseFlyout;

            var pasteButton = this.Get<Button>("PasteButton");
            pasteButton.Click += CloseFlyout;

            var clearButton = this.Get<Button>("ClearButton");
            clearButton.Click += CloseFlyout;

            var customContextRequestedBorder = this.Get<Border>("CustomContextRequestedBorder");
            customContextRequestedBorder.AddHandler(ContextRequestedEvent, CustomContextRequested, RoutingStrategies.Tunnel);

            var cancellableContextBorder = this.Get<Border>("CancellableContextBorder");
            var flyout = (Flyout)cancellableContextBorder.ContextFlyout!;
            flyout.Closing += ContextFlyoutPage_Closing;
            flyout.Opening += ContextFlyoutPage_Opening;
        }

        private ContextPageViewModel? _model;
        protected override void OnDataContextChanged(EventArgs e)
        {
            if (_model != null)
                _model.View = null;
            _model = DataContext as ContextPageViewModel;
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

        private void CloseFlyout(object? sender, RoutedEventArgs e)
        {
            _textBox.ContextFlyout?.Hide();
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
