using System;
using System.Globalization;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class CheckBoxTests : ScopedTestBase
    {
        [Fact]
        public void CheckBox_LetterSpacing_Default_Value_Is_Zero()
        {
            var checkBox = new CheckBox();
            Assert.Equal(0, checkBox.LetterSpacing);
        }

        [Fact]
        public void CheckBox_LetterSpacing_Can_Be_Set_And_Retrieved()
        {
            var checkBox = new CheckBox { LetterSpacing = 2.5 };
            Assert.Equal(2.5, checkBox.LetterSpacing);
        }

        [Fact]
        public void CheckBox_LetterSpacing_Inherits_From_TemplatedControl()
        {
            var checkBox = new CheckBox { LetterSpacing = 3.0 };
            // LetterSpacing is inherited from TemplatedControl
            Assert.Equal(3.0, checkBox.LetterSpacing);
        }

        [Fact]
        public void CheckBox_LetterSpacing_Can_Be_Negative()
        {
            var checkBox = new CheckBox { LetterSpacing = -1.5 };
            Assert.Equal(-1.5, checkBox.LetterSpacing);
        }

        [Fact]
        public void Removing_Checked_CheckBox_From_Tree_Should_Not_Write_IsChecked_Back_To_Source()
        {
            // Issue #22673
            var converter = new EqualsParameterConverter();
            var checkBox = new CheckBox();
            var host = new StackPanel
            {
                [!Control.TagProperty] = new Binding(nameof(ViewModel.Value)) { Mode = BindingMode.TwoWay },
                Children = { checkBox },
            };
            var parent = new StackPanel { Children = { host } };
            var root = new TestRoot(parent) { DataContext = new ViewModel { Value = "foo" } };

            checkBox.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(Control.Tag))
            {
                Source = host,
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = "foo",
            });

            Assert.True(checkBox.IsChecked);

            parent.Children.Remove(host);

            Assert.False(checkBox.IsChecked);
            Assert.Equal(0, converter.ConvertBackCount);
        }

        private class ViewModel
        {
            public object? Value { get; set; }
        }

        private class EqualsParameterConverter : IValueConverter
        {
            public int ConvertBackCount { get; private set; }

            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                return Equals(value, parameter);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                ++ConvertBackCount;
                return value is true ? parameter : null;
            }
        }
    }
}
