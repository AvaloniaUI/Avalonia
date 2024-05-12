using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ToolTipPage : UserControl
    {
        public ToolTipPage()
        {
            this.InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ToolTipOpening(object? sender, CancelRoutedEventArgs args)
        {
            ((Control)args.Source!).SetValue(ToolTip.TipProperty, "New tip set from ToolTipOpening.");
        } 
    }
}
