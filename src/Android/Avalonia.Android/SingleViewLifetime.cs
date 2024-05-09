using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android;

internal class SingleViewLifetime : ISingleViewApplicationLifetime, ISingleTopLevelApplicationLifetime
{
    private Control? _mainView;
    private AvaloniaMainActivity? _activity;
        
    /// <summary>
    /// Since Main Activity can be swapped, we should adjust litetime as well.  
    /// </summary>
    public AvaloniaMainActivity Activity
    {
        [return: MaybeNull] get => _activity!;
        internal set
        {
            if (_activity != null)
            {
                _activity.Content = null;
            }
            _activity = value;
            _activity.Content = _mainView;
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
                if (_activity != null)
                {
                    _activity.Content = _mainView;
                }
            }
        }
    }

    public TopLevel? TopLevel => _activity?._view?.TopLevel;
}
