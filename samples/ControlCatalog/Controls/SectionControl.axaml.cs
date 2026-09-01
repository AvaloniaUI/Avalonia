using System;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace ControlCatalog.Controls
{
    public partial class SectionControl : UserControl
    {
        public SectionControl()
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
