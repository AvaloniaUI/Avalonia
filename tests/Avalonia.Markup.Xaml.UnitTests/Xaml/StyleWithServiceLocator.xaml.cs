using System;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class StyleWithServiceLocator : Style
{
    public IServiceProvider ServiceProvider { get; }

    public StyleWithServiceLocator(IServiceProvider sp = null)
    {
        ServiceProvider = sp;
        AvaloniaXamlLoader.Load(sp, this);
    }
}
