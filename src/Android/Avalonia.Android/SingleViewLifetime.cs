using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android
{
    public class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        private AvaloniaView _view;

        public AvaloniaView View
        {
            get => _view; set
            {
                if (_view != null)
                {
                    _view.Content = null;
                    _view.Dispose();
                }
                _view = value;
                _view.Content = MainView;
            }
        }

        public Control MainView { get; set; }
    }
}
