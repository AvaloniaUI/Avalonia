using System.Linq;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Metadata;

namespace Avalonia.Diagnostics.ViewModels
{
    class EditFavoritesPropertiesViewModel : ViewModelBase
    {
        private AvaloniaList<EditFavoritePropertiesViewModel> _favorities;
        private EditFavoritePropertiesViewModel _selectedFavorite;
        private bool _isSelected;

        public AvaloniaList<EditFavoritePropertiesViewModel> Favorities
        {
            get => _favorities;
            set
            {
                RaiseAndSetIfChanged(ref _favorities, value);
            }
        }

        public EditFavoritePropertiesViewModel SelectedFavorite
        {
            get => _selectedFavorite;
            set
            {
                RaiseAndSetIfChanged(ref _selectedFavorite, value);
                IsSelected = value != null;
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            private set
            {
                RaiseAndSetIfChanged(ref _isSelected, value);
            }
        }

        void Add(object parameter)
        {
            var favorite = new EditFavoritePropertiesViewModel()
            {
                Name = $"Item {Favorities.Count}",
                Properies = new string[0],
            };
            Favorities.Add(favorite);
            SelectedFavorite = favorite;
        }

        [DependsOn(nameof(SelectedFavorite))]
        bool CanRemove(object parameter) => (parameter is EditFavoritePropertiesViewModel);

        void Remove(object parameter)
        {
            if (parameter is EditFavoritePropertiesViewModel model)
            {
                Favorities.Remove(model);
            }
        }

        void Accept(object parameter)
        {
            if (parameter is Window dialog)
            {
                var result = Favorities.Select(fav => new Models.FavoriteProperties()
                {
                    Name = fav.Name,
                    Properties = fav.Properies.ToArray(),
                }).ToArray();
                dialog.Close(result);
            }
        }

        void Cancel(object parameter)
        {
            if (parameter is Window dialog)
            {
                dialog.Close();
            }
        }

        void Restore(object parameter)
        {
            if (parameter is Window dialog)
            {
                dialog.Close(Models.FavoriteProperties.Default);
            }
        }
    }
}
