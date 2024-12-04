using Android.App;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android.Platform;

internal class AndroidActivatableLifetime : ActivatableLifetimeBase
{
    private IAvaloniaActivity? _mainActivity, _intendActivity;

    /// <summary>
    /// While we primarily handle main activity lifecycle events.
    /// Any secondary activity might send protocol or file activation.
    /// </summary>
    public IAvaloniaActivity? CurrentIntendActivity
    {
        get => _intendActivity;
        set
        {
            if (_intendActivity is not null)
            {
                _intendActivity.Activated -= IntendActivityOnActivated;
            }

            _intendActivity = value;

            if (_intendActivity is not null)
            {
                _intendActivity.Activated += IntendActivityOnActivated;
            }
        }
    }
    
    public IAvaloniaActivity? CurrentMainActivity
    {
        get => _mainActivity;
        set
        {
            if (_mainActivity is not null)
            {
                _mainActivity.Activated -= MainActivityOnActivated;
                _mainActivity.Deactivated -= MainActivityOnDeactivated;
            }

            _mainActivity = value;

            if (_mainActivity is not null)
            {
                _mainActivity.Activated += MainActivityOnActivated;
                _mainActivity.Deactivated += MainActivityOnDeactivated;
            }
        }
    }

    public override bool TryEnterBackground() => (_mainActivity as Activity)?.MoveTaskToBack(true) == true;

    private void MainActivityOnDeactivated(object? sender, ActivatedEventArgs e) => OnDeactivated(e);

    private void MainActivityOnActivated(object? sender, ActivatedEventArgs e)
    {
        if (!IsIntendActivation(e.Kind))
        {
            OnActivated(e);
        }
    }

    private void IntendActivityOnActivated(object? sender, ActivatedEventArgs e)
    {
        if (IsIntendActivation(e.Kind))
        {
            OnActivated(e);
        }
    }

    private static bool IsIntendActivation(ActivationKind kind) => kind is ActivationKind.File or ActivationKind.OpenUri;
}
