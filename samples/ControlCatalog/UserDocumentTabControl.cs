using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;
using System;
using System.IO;

namespace ControlCatalog
{
    public class UserDocumentTabControl : UserControl
    {
        public UserDocumentTabControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}