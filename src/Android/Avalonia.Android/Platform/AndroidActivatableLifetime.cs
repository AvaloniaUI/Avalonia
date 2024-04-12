using Android.App;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android.Platform;

internal class AndroidActivatableLifetime : ActivatableLifetimeBase
{
    private IAvaloniaActivity? _activity;

    public IAvaloniaActivity? Activity
    {
        get => _activity;
        set
        {
            if (_activity is not null)
            {
                _activity.Activated -= ActivityOnActivated;
                _activity.Deactivated -= ActivityOnDeactivated;
            }

            _activity = value;

            if (_activity is not null)
            {
                _activity.Activated += ActivityOnActivated;
                _activity.Deactivated += ActivityOnDeactivated;
            }
        }
    }

    public override bool TryEnterBackground() => (_activity as Activity)?.MoveTaskToBack(true) == true;

    private void ActivityOnDeactivated(object? sender, ActivatedEventArgs e) => OnDeactivated(e);

    private void ActivityOnActivated(object? sender, ActivatedEventArgs e) => OnActivated(e);
}
