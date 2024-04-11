using System;
using System.Collections.Generic;
using Avalonia.Controls.Platform;
using Avalonia.Metadata;
using Avalonia.Platform.Storage;
using Avalonia.Threading;

namespace Avalonia.Controls.ApplicationLifetimes;

[PrivateApi]
public abstract class ActivatableLifetimeBase : IActivatableLifetime
{
    public event EventHandler<ActivatedEventArgs>? Activated;
    public event EventHandler<ActivatedEventArgs>? Deactivated;

    public virtual bool TryLeaveBackground() => false;
    public virtual bool TryEnterBackground() => false;

    protected internal void OnActivated(ActivationKind kind) => OnActivated(new ActivatedEventArgs(kind));

    protected internal void OnActivated(ActivatedEventArgs eventArgs) =>
        Dispatcher.UIThread.Send(_ => Activated?.Invoke(this, eventArgs));

    protected internal void OnDeactivated(ActivationKind kind) => OnDeactivated(new ActivatedEventArgs(kind));

    protected internal void OnDeactivated(ActivatedEventArgs eventArgs) =>
        Dispatcher.UIThread.Send(_ => Deactivated?.Invoke(this, eventArgs));
}
