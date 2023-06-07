using Avalonia;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class ExpanderPageViewModel : ViewModelBase
    {
        private object _cornerRadius = AvaloniaProperty.UnsetValue;
        private bool _rounded;

        public object CornerRadius
        {
            get => _cornerRadius;
            private set => RaiseAndSetIfChanged(ref _cornerRadius, value);
        }

        public bool Rounded
        {
            get => _rounded;
            set
            {
                if (RaiseAndSetIfChanged(ref _rounded, value))
                    CornerRadius = _rounded ? new CornerRadius(25) : AvaloniaProperty.UnsetValue;
            }
        }
    }
}
