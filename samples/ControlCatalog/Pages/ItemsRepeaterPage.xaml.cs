using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages
{
    public class ItemsRepeaterPage : UserControl
    {
        public ItemsRepeaterPage()
        {
            this.InitializeComponent();
            DataContext = Enumerable.Range(1, 100000).Select(i => $"Item {i}" )
                .ToArray();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

    }
}
