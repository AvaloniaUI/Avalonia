using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;

namespace Avalonia.Themes.Simple;

public class SimpleTheme : Styles
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleTheme"/> class.
    /// </summary>
    /// <param name="sp">The parent's service provider.</param>
    public SimpleTheme(IServiceProvider? sp = null)
    {
        AvaloniaXamlLoader.Load(sp, this);
    }
}
