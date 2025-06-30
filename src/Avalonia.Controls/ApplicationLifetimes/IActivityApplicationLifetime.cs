using System;
using Avalonia.Metadata;

namespace Avalonia.Controls.ApplicationLifetimes
{
    [NotClientImplementable]
    public interface IActivityApplicationLifetime : IApplicationLifetime
    {
        Func<Control>? MainViewFactory { get; set; }
    }
}
