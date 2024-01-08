using System;

namespace Avalonia.Controls.ApplicationLifetimes;

public class ActivatedEventArgs : EventArgs
{
    public ActivatedEventArgs(ActivationKind kind)
    {
        Kind = kind;
    }
        
    public ActivationKind Kind { get; }
}
