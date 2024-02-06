using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes;

/// <summary>
/// An interface for ApplicationLifetimes where the application can be Activated and Deactivated.
/// </summary>
[NotClientImplementable]
public interface IActivatableApplicationLifetime
{
    /// <summary>
    /// An event that is raised when the application is Activated for various reasons
    /// as described by the <see cref="ActivationKind"/> enumeration.
    /// </summary>
    event EventHandler<ActivatedEventArgs>? Activated;
    
    /// <summary>
    /// An event that is raised when the application is Deactivated for various reasons
    /// as described by the <see cref="ActivationKind"/> enumeration.
    /// </summary>
    event EventHandler<ActivatedEventArgs>? Deactivated;

    /// <summary>
    /// Tells the application that it should attempt to leave its background state.
    /// For example on OSX this would be [NSApp unhide]
    /// </summary>
    /// <returns>true if it was possible and the platform supports this. false otherwise</returns>
    public bool TryLeaveBackground();

    /// <summary>
    /// Tells the application that it should attempt to enter its background state.
    /// For example on OSX this would be [NSApp hide]
    /// </summary>
    /// <returns>true if it was possible and the platform supports this. false otherwise</returns>
    public bool TryEnterBackground();
}
