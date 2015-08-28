using System;
using Perspex.Controls;
using Perspex.Xaml.Desktop;

namespace ApplicationTemplate
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            App.AttachDevTools(this);
        }

        private void InitializeComponent()
        {
            var loader = new PerspexXamlLoader(new PerspexInflatableTypeFactory());
            loader.Load(this.GetType());
        }
    }
}
