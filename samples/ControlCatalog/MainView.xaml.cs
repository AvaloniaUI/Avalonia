using System;
using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Platform;
using Avalonia.Themes.Fluent;
using ControlCatalog.Models;
using ControlCatalog.Pages;

namespace ControlCatalog
{
    public class MainView : UserControl
    {
        public MainView()
        {
            AvaloniaXamlLoader.Load(this);

            Console.WriteLine("Main view loaded");
        }
    }
}
