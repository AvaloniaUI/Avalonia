using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android
{
    internal class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        private AvaloniaView _view;

        public AvaloniaView View
        {
            get => _view; internal set
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
