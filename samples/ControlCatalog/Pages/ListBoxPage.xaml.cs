using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ListBoxPage : UserControl
    {
        public ListBoxPage()
        {
            this.InitializeComponent();
            DataContext = Enumerable.Range(1, 10).Select(i => $"Item {i}" )
                .ToArray();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
