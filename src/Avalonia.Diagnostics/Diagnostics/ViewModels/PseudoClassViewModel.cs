using Avalonia.Controls;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class PseudoClassViewModel : ViewModelBase
    {
        private readonly IPseudoClasses _pseudoClasses;
        private readonly StyledElement _source;
        private bool _isActive;
        private bool _isUpdating;

        public PseudoClassViewModel(string name, StyledElement source)
        {
            Name = name;
            _source = source;
            _pseudoClasses = _source.Classes;

            Update();
        }

        public string Name { get; }

        public bool IsActive
        {
            get => _isActive;
            set
            {
                RaiseAndSetIfChanged(ref _isActive, value);

                if (!_isUpdating)
                {
                    _pseudoClasses.Set(Name, value);
                }
            }
        }

        public void Update()
        {
            try
            {
                _isUpdating = true;

                IsActive = _source.Classes.Contains(Name);
            }
            finally
            {
                _isUpdating = false;
            }
        }
    }
}
