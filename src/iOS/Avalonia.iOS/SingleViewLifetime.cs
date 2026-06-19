using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.iOS;

internal class SingleViewLifetime : ISingleViewApplicationLifetime, ISingleTopLevelApplicationLifetime
{
    private Control? _mainView;
    private AvaloniaView? _view;

    public AvaloniaView? View
    {
        get => _view;
        internal set
        {
            if (_view != null)
            {
                _view.Content = null;
                _view.Dispose();
            }
            _view = value;
            _view?.Content = _mainView;
        }
    }

    public Control? MainView
    {
        get => _mainView;
        set
        {
            if (_mainView != value)
            {
                _mainView = value;
                _view?.Content = _mainView;
            }
        }
    }

    public TopLevel? TopLevel => View?.TopLevel;
}
