using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace Avalonia.Android;

internal class SingleViewSourceLifetime : ISingleViewFactoryApplicationLifetime
{
    public Func<Control>? MainViewFactory { get; set; }
}
