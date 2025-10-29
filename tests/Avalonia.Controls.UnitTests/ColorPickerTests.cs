using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ColorPickerTests : ScopedTestBase
    {
        private static FuncControlTemplate GetTemplate()
        {
            var tabCtrl = new TabControl
            {
                Name = "PART_TabControl",
            };
            tabCtrl.Items.Add(new TabItem() { Header = "ColorSpectrum" });
            tabCtrl.Items.Add(new TabItem() { Header = "ColorPalette" });
            tabCtrl.Items.Add(new TabItem() { Header = "ColorComponents" });

            return new FuncControlTemplate<ColorPicker>((parent, scope) =>
            {
                return new DropDownButton
                {
                    Flyout = new Flyout
                    {
                        Content = tabCtrl.RegisterInNameScope(scope)
                    }
                };
            });
        }

        [Fact]
        public void Setting_SelectedIndex_Before_Initialize_Should_Retain_Selection()
        {
            var colorPicker = new ColorPicker
            {
                SelectedIndex = 1,
                Template = GetTemplate()
            };

            colorPicker.BeginInit();

            var root = new TestRoot(colorPicker);
            colorPicker.ApplyTemplate();

            colorPicker.EndInit();

            Assert.Equal(1, colorPicker.SelectedIndex);
        }
    }
}
