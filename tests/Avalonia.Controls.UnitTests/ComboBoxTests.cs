using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Media;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ComboBoxTests
    {
        [Fact]
        public void Text_Is_Selected_Item_Content()
        {
            ComboBoxItem[] items = new ComboBoxItem[]
            {
                new ComboBoxItem { Content = "A" },
                new ComboBoxItem { Content = "B" },
                new ComboBoxItem { Content = "C" },
                new ComboBoxItem { Content = "D" }
            };

            var target = new ComboBox
            {
                Items = items,
                SelectedIndex = 2
            };

            var text = target.GetValue(ComboBox.TextProperty);
            Assert.True(text == "C");
        }
    }
}
