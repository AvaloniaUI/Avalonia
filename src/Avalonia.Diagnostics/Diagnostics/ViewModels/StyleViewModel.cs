using System.Collections.Generic;
using Avalonia.Styling;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class StyleViewModel : ViewModelBase
    {
        private readonly AppliedStyle _styleInstance;
        private bool _isActive;
        private bool _isVisible;

        public StyleViewModel(AppliedStyle styleInstance, string name, List<SetterViewModel> setters)
        {
            _styleInstance = styleInstance;
            IsVisible = true;
            Name = name;
            Setters = setters;

            Update();
        }

        public bool IsActive
        {
            get => _isActive;
            set => RaiseAndSetIfChanged(ref _isActive, value);
        }

        public bool IsVisible
        {
            get => _isVisible;
            set => RaiseAndSetIfChanged(ref _isVisible, value);
        }

        public string Name { get; }

        public List<SetterViewModel> Setters { get; }

        public void Update()
        {
            IsActive = _styleInstance.IsActive;
        }
    }
}
