using System;
using System.Linq;
using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes;

public sealed class ProtocolActivatedEventArgs : ActivatedEventArgs
{
    public ProtocolActivatedEventArgs(Uri uri) : base(ActivationKind.OpenUri)
    {
        Uri = uri;
    }

    public Uri Uri { get; }
}
