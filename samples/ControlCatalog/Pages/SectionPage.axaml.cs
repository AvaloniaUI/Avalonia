using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using ControlCatalog.Models;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class SectionPage : ContentPage
    {
        public SectionPage(HomeSection homeSection)
        {
            InitializeComponent();

            DataContext = new SectionViewModel(homeSection);
        }

        public SectionPage()
        {
            InitializeComponent();
        }

        private void UniformGrid_OnSizeChanged(object? sender, SizeChangedEventArgs e)
        {
            if (sender is not UniformGrid grid)
                return;

            const int minItemWidth = 248;
            grid.Columns = Math.Max(
                1,
                (int)((e.NewSize.Width + grid.ColumnSpacing) / (minItemWidth + grid.ColumnSpacing)));
        }
    }
}
