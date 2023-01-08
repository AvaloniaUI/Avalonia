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

        public bool WrapSelection
        {
            get => _wrapSelection;
            set => this.RaiseAndSetIfChanged(ref _wrapSelection, value);
        }
    }
}
