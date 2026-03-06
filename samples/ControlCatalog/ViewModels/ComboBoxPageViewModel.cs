using System;
using System.Collections.ObjectModel;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Selection;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ComboBoxPageViewModel : ViewModelBase
    {
        private bool _wrapSelection;
        private string _textValue = string.Empty;
        private IdAndName? _selectedItem = null;

        public bool WrapSelection
        {
            get => _wrapSelection;
            set => this.RaiseAndSetIfChanged(ref _wrapSelection, value);
        }

        public string TextValue
        {
            get => _textValue;
            set => this.RaiseAndSetIfChanged(ref _textValue, value);
        }

        public IdAndName? SelectedItem
        {
            get => _selectedItem;
            set => this.RaiseAndSetIfChanged(ref _selectedItem, value);
        }

        public ObservableCollection<IdAndName> Values { get; set; } = new ObservableCollection<IdAndName>
        {
            new IdAndName(){ Id = "Id 1", Name = "Name 1", SearchText = "A" },
            new IdAndName(){ Id = "Id 2", Name = "Name 2", SearchText = "B" },
            new IdAndName(){ Id = "Id 3", Name = "Name 3", SearchText = "C" },
            new IdAndName(){ Id = "Id 4", Name = "Name 4", SearchText = "D" },
            new IdAndName(){ Id = "Id 5", Name = "Name 5", SearchText = "E" },
        };
    }

    public class IdAndName
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? SearchText { get; set; }
    }
}
