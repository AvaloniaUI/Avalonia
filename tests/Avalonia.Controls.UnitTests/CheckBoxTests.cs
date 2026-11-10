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


        [Theory]
        [InlineData(CheckboxDetachMode.ParentChildren_Remove, false)]
        [InlineData(CheckboxDetachMode.ParentChildren_Clear, false)]
        [InlineData(CheckboxDetachMode.HostChildren_Remove, false)]
        [InlineData(CheckboxDetachMode.HostChildren_Clear, false)]
        [InlineData(CheckboxDetachMode.ParentChildren_Remove, true)]
        [InlineData(CheckboxDetachMode.ParentChildren_Clear, true)]
        [InlineData(CheckboxDetachMode.HostChildren_Remove, true)]
        [InlineData(CheckboxDetachMode.HostChildren_Clear, true)]
        public void Removing_Checked_CheckBox_From_Tree_Should_Not_Write_IsChecked_Back_To_Source(CheckboxDetachMode detachMode, bool unsetForNull)
        {
            // Issue #22673
            var converter = new EqualsParameterConverter { UnsetForNull = unsetForNull };
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

            switch (detachMode)
            {
                case CheckboxDetachMode.ParentChildren_Remove:
                    parent.Children.Remove(host);
                    break;
                case CheckboxDetachMode.ParentChildren_Clear:
                    parent.Children.Clear();
                    break;
                case CheckboxDetachMode.HostChildren_Remove:
                    host.Children.Remove(checkBox); // checkbox is removed from tree,
                                                    // but 'host' (binding's Target) is still in tree
                    break;
                case CheckboxDetachMode.HostChildren_Clear:
                    host.Children.Clear(); // checkbox is removed from tree,
                                           // but 'host' (binding's Target) is still in tree
                    break;
            }

            // When only the CheckBox is detached, its binding source (host.Tag) is unchanged, so
            // IsChecked keeps its value. Otherwise host loses its DataContext and IsChecked is reset.
            var hostInTree = detachMode is CheckboxDetachMode.HostChildren_Remove or CheckboxDetachMode.HostChildren_Clear;
            Assert.Equal(hostInTree, checkBox.IsChecked);
            Assert.Equal(0, converter.ConvertBackCount);
        }

        [Fact]
        public void Removing_Checked_CheckBox_WithSelfBinding_From_Tree_Should_Not_Write_IsChecked_Back_To_Source()
        {
            // Issue #22673 - Partial case when checkbox and binding's target are same instances.
            var converter = new EqualsParameterConverter();
            var checkBox = new CheckBox()
            {
                [!Control.TagProperty] = new Binding(nameof(ViewModel.Value)) { Mode = BindingMode.TwoWay },
            };
            var host = new StackPanel
            {
                Children = { checkBox },
            };
            var parent = new StackPanel { Children = { host } };
            var viewModel = new ViewModel { Value = null };
            var root = new TestRoot(parent) { DataContext = viewModel };

            checkBox.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(Control.Tag))
            {
                Source = checkBox, // bind to itself
                Mode = BindingMode.TwoWay,
                Converter = converter,
                ConverterParameter = "foo",
            });


            Assert.Equal(0, converter.ConvertBackCount);
            checkBox.IsChecked = true;

            Assert.Equal(1, converter.ConvertBackCount);
            Assert.Equal("foo", viewModel.Value);

            Assert.True(checkBox.IsChecked);

            host.Children.Clear();

            Assert.False(checkBox.IsChecked);
            Assert.Equal(1, converter.ConvertBackCount);
            Assert.Equal("foo", viewModel.Value);
        }

        private class ViewModel
        {
            public object? Value { get; set; }
        }

        private class EqualsParameterConverter : IValueConverter
        {
            public int ConvertBackCount { get; private set; }
            public bool UnsetForNull { get; set; }

            public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                if (value is null && UnsetForNull)
                    return AvaloniaProperty.UnsetValue;
                return Equals(value, parameter);
            }

            public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            {
                ++ConvertBackCount;
                return value is true ? parameter : null;
            }
        }

        public enum CheckboxDetachMode
        {
            ParentChildren_Remove,
            ParentChildren_Clear,
            HostChildren_Clear,
            HostChildren_Remove,
        }
    }
}
