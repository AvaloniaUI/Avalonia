using Avalonia.Input.Platform;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class SetterViewModel : ViewModelBase
    {
        private bool _isActive;
        private bool _isVisible;

        public AvaloniaProperty Property { get; }

        public string Name { get; }

        public object? Value { get; }

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

        private IClipboard? _clipboard;

        public SetterViewModel(AvaloniaProperty property, object? value, IClipboard? clipboard)
        {
            Property = property;
            Name = property.Name;
            Value = value;
            IsActive = true;
            IsVisible = true;

            _clipboard = clipboard;
        }

        public virtual void CopyValue()
        {
            var textToCopy = Value?.ToString();

            if (textToCopy is null)
            {
                return;
            }

            CopyToClipboard(textToCopy);
        }

        public void CopyPropertyName()
        {
            CopyToClipboard(Property.Name);
        }

        protected void CopyToClipboard(string value)
        {
            _clipboard?.SetTextAsync(value);
        }
    }
}
