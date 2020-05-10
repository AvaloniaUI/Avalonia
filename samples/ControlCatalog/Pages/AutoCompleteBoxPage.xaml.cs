using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using Avalonia.Data;

namespace ControlCatalog.Pages
{
    public class AutoCompleteBoxPage : UserControl
    {

        public static readonly StyledProperty<bool> SideBarEnabledProperty =
            AvaloniaProperty.Register<MetroWindow, bool>(nameof(SideBarEnabled));

        public bool SideBarEnabled
        {
            get => GetValue(SideBarEnabledProperty);
            set => SetValue(SideBarEnabledProperty, value);
        }

        public AutoCompleteBoxPage()
        {
            this.InitializeComponent();
            this.FindControl<Button>("sidebar").Click += delegate
            {
                SideBarEnabled = !SideBarEnabled;
            };
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
