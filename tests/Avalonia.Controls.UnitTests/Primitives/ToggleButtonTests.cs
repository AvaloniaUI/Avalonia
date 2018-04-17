using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;

using Xunit;

namespace Avalonia.Controls.Primitives.UnitTests
{
    public class ToggleButtonTests
    {
        private const string uncheckedClass = ":unchecked";
        private const string checkedClass = ":checked";
        private const string indeterminateClass = ":indeterminate";

        [Theory]
        [InlineData(false, uncheckedClass, false)]
        [InlineData(false, uncheckedClass, true)]
        [InlineData(true, checkedClass, false)]
        [InlineData(true, checkedClass, true)]
        [InlineData(null, indeterminateClass, false)]
        [InlineData(null, indeterminateClass, true)]
        public void ToggleButton_Has_Correct_Class_According_To_Is_Checked(bool? isChecked, string expectedClass, bool isThreeState)
        {
            var toggleButton = new ToggleButton();
            toggleButton.IsThreeState = isThreeState;
            toggleButton.IsChecked = isChecked;

            Assert.Contains(expectedClass, toggleButton.Classes);
        }

        [Fact]
        public void ToggleButton_Is_Checked_Binds_To_Bool()
        {
            var toggleButton = new ToggleButton();
            var source = new Class1();

            toggleButton.DataContext = source;
            toggleButton.Bind(ToggleButton.IsCheckedProperty, new Binding("Foo"));

            source.Foo = true;
            Assert.True(toggleButton.IsChecked);

            source.Foo = false;
            Assert.False(toggleButton.IsChecked);
        }

        private class Class1 : NotifyingBase
        {
            private bool _foo;

            public bool Foo
            {
                get { return _foo; }
                set { _foo = value; RaisePropertyChanged(); }
            }
        }
    }
}
