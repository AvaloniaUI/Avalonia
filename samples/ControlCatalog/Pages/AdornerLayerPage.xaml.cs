using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace ControlCatalog.Pages
{
    public partial class AdornerLayerPage : UserControl
    {
        private Control? _adorner;

        public AdornerLayerPage()
        {
            InitializeComponent();
        }

        private void RemoveAdorner_OnClick(object? sender, RoutedEventArgs e)
        {
            var adorner = AdornerLayer.GetAdorner(AdornerButton);
            if (adorner is { })
            {
                _adorner = adorner;
            }
            AdornerLayer.SetAdorner(AdornerButton, null);
        }

        private void AddAdorner_OnClick(object? sender, RoutedEventArgs e)
        {
            if (_adorner is { })
            {
                AdornerLayer.SetAdorner(AdornerButton, _adorner);
            }
        }
    }
}
