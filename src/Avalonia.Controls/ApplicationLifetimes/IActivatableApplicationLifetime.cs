using System;

namespace Avalonia.Controls.ApplicationLifetimes;

public interface IActivatableApplicationLifetime
{
    event EventHandler<ActivatedEventArgs> Activated;
    
    event EventHandler<ActivatedEventArgs> Deactivated;
}
