using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.Linq;

namespace Avalonia.Diagnostics.Views
{
    class EditFavoritesPropertiesDialog : Window
    {
        static readonly string[] Empty = new string[0];
        public EditFavoritesPropertiesDialog(System.Collections.Generic.IEnumerable<Models.FavoriteProperties> favorites):base()
        {
            this.InitializeComponent();
            this.AttachDevTools();

            var items =  favorites.Select(f => new ViewModels.EditFavoritePropertiesViewModel()
            {
                Name = f.Name,
                Properies = f.Properties ?? Empty,
            });

            (DataContext as ViewModels.EditFavoritesPropertiesViewModel).Favorities =
                new AvaloniaList<ViewModels.EditFavoritePropertiesViewModel>(items);
                
        }
        public EditFavoritesPropertiesDialog()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
