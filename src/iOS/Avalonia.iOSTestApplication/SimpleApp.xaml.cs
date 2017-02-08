using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Avalonia.Markup.Xaml;
using Foundation;
using UIKit;

namespace Avalonia.iOSTestApplication
{
    public class SimpleApp : Avalonia.Application
    {
        public override void Initialize()
        {
            //Enforce load
            new Avalonia.Themes.Default.DefaultTheme();
            AvaloniaXamlLoader.Load(this);
        }
    }
}