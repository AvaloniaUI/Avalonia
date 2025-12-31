using System;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class ContextFlyoutPage : UserControl
    {
        public ContextFlyoutPage()
        {
            InitializeComponent();
            DataContext = new ContextPageViewModel();
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
            e.Cancel = CancelCloseCheckBox?.IsChecked ?? false;
        }

        private void ContextFlyoutPage_Opening(object? sender, EventArgs e)
        {
            if (e is CancelEventArgs cancelArgs)
            {
                cancelArgs.Cancel = CancelCloseCheckBox?.IsChecked ?? false;
            }
        }

        private void CloseFlyout(object? sender, RoutedEventArgs e)
        {
            TextBox.ContextFlyout?.Hide();
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
    }
}
