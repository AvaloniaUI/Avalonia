using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ListBoxPage : UserControl
    {
        private readonly ListBox _listBox;
        private readonly TextBlock _realizedItemCount;
        private readonly DispatcherTimer _timer;

        public ListBoxPage()
        {
            InitializeComponent();
            DataContext = new ListBoxPageViewModel();
            _listBox = this.FindControl<ListBox>("listBox");
            _realizedItemCount = this.FindControl<TextBlock>("realizedItemCount");
            _timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Background, UpdateRealizedItems);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _timer.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _timer.Stop();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void UpdateRealizedItems(object sender, EventArgs e)
        {
            _realizedItemCount.Text = $"Realized items: {_listBox.Presenter.Panel.Children.Count}";
        }
    }
}
