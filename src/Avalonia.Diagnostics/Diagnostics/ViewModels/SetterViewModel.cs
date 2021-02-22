namespace Avalonia.Diagnostics.ViewModels
{
    internal class SetterViewModel : ViewModelBase
    {
        private bool _isActive;
        private bool _isVisible;

        public AvaloniaProperty Property { get; }

        public string Name { get; }

        public object Value { get; }

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

        public SetterViewModel(AvaloniaProperty property, object value)
        {
            Property = property;
            Name = property.Name;
            Value = value;
            IsActive = true;
            IsVisible = true;
        }
    }
}
