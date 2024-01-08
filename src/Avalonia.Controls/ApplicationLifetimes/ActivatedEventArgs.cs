using System;

namespace Avalonia.Controls.ApplicationLifetimes;

public class ActivatedEventArgs : EventArgs
{
    public ActivatedEventArgs(ActivationReason reason)
    {
        Reason = reason;
    }
        
    public ActivationReason Reason { get; }
}
