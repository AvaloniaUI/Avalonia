using System;

namespace Avalonia.Controls.ApplicationLifetimes;

public class ProtocolActivatedEventArgs : ActivatedEventArgs
{
    public ProtocolActivatedEventArgs(ActivationKind kind, Uri uri) : base(kind)
    {
        Uri = uri;
    }

    public Uri Uri { get; }
}
