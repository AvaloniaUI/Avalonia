using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ComboBoxTests
    {
        [Fact]
        public void Text_Is_Content_Of_Selected_ComboBoxItem()
        {
            var items = new ComboBoxItem[]
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

        [Fact]
        public void Can_Bind_Values_Convertible_To_String()
        {
            var items = new object[]
            {
                1,
                "2",
                5.0m,
                -3.0,
                'a'
            };

            var target = new ComboBox
            {
                Items = items,
                SelectedIndex = 2
            };

            var text = target.GetValue(ComboBox.TextProperty);
            Assert.True(text == 5.0m.ToString());
        }
    }
}
