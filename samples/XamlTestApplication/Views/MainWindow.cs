namespace XamlTestApplication.Views
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Resources;
    using OmniXaml;
    using Perspex.Controls;
    using Perspex.Diagnostics;
    using Perspex.Xaml;

    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            DevTools.Attach(this);
        }

        private void InitializeComponent()
        {
            var xamlLoader = new PerspexXamlLoader();
            xamlLoader.Load(new Uri("Views/MainWindow.xaml", UriKind.Relative), this);
        }
    }
}