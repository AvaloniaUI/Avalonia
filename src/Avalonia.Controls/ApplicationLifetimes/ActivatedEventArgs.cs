using System;

namespace Avalonia.Controls.ApplicationLifetimes;

/// <summary>
/// Event args for an Application Lifetime Activated or Deactivated events.
/// </summary>
public class ActivatedEventArgs : EventArgs
{
    /// <summary>
    /// Ctor for ActivatedEventArgs
    /// </summary>
    /// <param name="kind">The <see cref="ActivationKind"/> that this event represents</param>
    public ActivatedEventArgs(ActivationKind kind)
    {
        Kind = kind;
    }
      
    /// <summary>
    /// The <see cref="ActivationKind"/> that this event represents.
    /// </summary>
    public ActivationKind Kind { get; }
}
