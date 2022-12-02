using System;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml;

public class StyleWithServiceProvider : Style
{
    public IServiceProvider ServiceProvider { get; }

    public StyleWithServiceProvider(IServiceProvider sp = null)
    {
        ServiceProvider = sp;
        AvaloniaXamlLoader.Load(sp, this);
    }
}
