using System;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android;

internal class ApplicationLifetime : IActivityApplicationLifetime
{
    public Func<Control>? MainViewFactory { get; set; }
}
