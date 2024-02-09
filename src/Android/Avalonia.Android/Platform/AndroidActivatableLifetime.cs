using System;
using Android.App;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android.Platform;

internal class AndroidActivatableLifetime : IActivatableLifetime
{
    private readonly IAvaloniaActivity _activity;

    public AndroidActivatableLifetime(IAvaloniaActivity activity)
    {
        _activity = activity;
        _activity.Activated += (_, args) => Activated?.Invoke(this, args);
        _activity.Deactivated += (_, args) => Deactivated?.Invoke(this, args);
    }

    public event EventHandler<ActivatedEventArgs> Activated;
    public event EventHandler<ActivatedEventArgs> Deactivated;

    public bool TryLeaveBackground() => (_activity as Activity)?.MoveTaskToBack(true) == true;
    public bool TryEnterBackground() => false;
}
