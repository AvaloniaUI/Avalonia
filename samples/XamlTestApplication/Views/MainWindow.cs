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
    using Perspex.Xaml.Desktop;

    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            DevTools.Attach(this);
        }

        private void InitializeComponent()
        {
            var xamlLoader = new XamlLoader(new PerspexParserFactory(new TypeFactory()));
            var stream = GetStream(new Uri("Views/MainWindow.xaml", UriKind.Relative));
            xamlLoader.Load(stream, this);
        }


        private static Stream GetStream(Uri resourceLocator)
        {
            var assembly = Assembly.GetEntryAssembly();
            var resourceName = assembly.GetName().Name + ".g";
            var manager = new ResourceManager(resourceName, assembly);

            using (var resourceSet = manager.GetResourceSet(CultureInfo.CurrentCulture, true, true))
            {
                var stream = (Stream)resourceSet.GetObject(resourceLocator.ToString(), true);

                if (stream == null)
                {
                    throw new IOException($"The requested resource could not be found: {resourceLocator}");
                }

                return stream;
            }
        }
    }
}