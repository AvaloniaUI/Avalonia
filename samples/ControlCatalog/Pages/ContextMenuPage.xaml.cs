using System;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class ContextMenuPage : UserControl
    {
        public ContextMenuPage()
        {
            this.InitializeComponent();
            DataContext = new ContextMenuPageViewModel();
        }

        private ContextMenuPageViewModel _model;
        protected override void OnDataContextChanged(EventArgs e)
        {
            if (_model != null)
                _model.View = null;
            _model  = DataContext as ContextMenuPageViewModel;
            if (_model != null)
                _model.View = this;

            base.OnDataContextChanged(e);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
