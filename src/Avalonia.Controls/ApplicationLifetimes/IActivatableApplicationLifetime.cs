using System;

namespace Avalonia.Controls.ApplicationLifetimes;

/// <summary>
/// An interface for ApplicationLifetimes where the application can be Activated and Deactivated.
/// </summary>
public interface IActivatableApplicationLifetime
{
    /// <summary>
    /// An event that is raised when the application is Activated for various reasons
    /// as described by the <see cref="ActivationKind"/> enumeration.
    /// </summary>
    event EventHandler<ActivatedEventArgs> Activated;
    
    /// <summary>
    /// An event that is raised when the application is Deactivated for various reasons
    /// as described by the <see cref="ActivationKind"/> enumeration.
    /// </summary>
    event EventHandler<ActivatedEventArgs> Deactivated;
}
