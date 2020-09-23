using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views
{
    class EditFavoritesPropertiesView : UserControl
    {
        public EditFavoritesPropertiesView()
        {
            this.InitializeComponent();            
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
