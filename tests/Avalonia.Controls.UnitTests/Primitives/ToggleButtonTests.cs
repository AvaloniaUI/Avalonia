using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.Primitives.UnitTests
{
    public class ToggleButtonTests : ScopedTestBase
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

        [Fact]
        public void ToggleButton_ThreeState_Checked_Binds_To_Nullable_Bool()
        {
            var threeStateButton = new ToggleButton();
            var source = new Class1();

            threeStateButton.DataContext = source;
            threeStateButton.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(Class1.NullableFoo)));

            source.NullableFoo = true;
            Assert.True(threeStateButton.IsChecked);

            source.NullableFoo = false;
            Assert.False(threeStateButton.IsChecked);

            source.NullableFoo = null;
            Assert.Null(threeStateButton.IsChecked);
        }

        [Fact]
        public void ToggleButton_IsCheckedChanged_Is_Raised_On_Is_Checked_Changes()
        {
            var threeStateButton = new ToggleButton();
            Assert.False(threeStateButton.IsChecked);

            var changeCount = 0;
            threeStateButton.IsCheckedChanged += (_, _) => ++changeCount;

            threeStateButton.IsChecked = true;
            Assert.Equal(1, changeCount);
            Assert.True(threeStateButton.IsChecked);

            threeStateButton.IsChecked = false;
            Assert.Equal(2, changeCount);
            Assert.False(threeStateButton.IsChecked);

            threeStateButton.IsChecked = null;
            Assert.Equal(3, changeCount);
            Assert.Null(threeStateButton.IsChecked);
        }

        [Fact]
        public void ToggleButton_IsCheckedChanged_Is_Raised_When_Toggling()
        {
            var threeStateButton = new TestToggleButton { IsThreeState = true };
            Assert.False(threeStateButton.IsChecked);

            var changeCount = 0;
            threeStateButton.IsCheckedChanged += (_, _) => ++changeCount;

            threeStateButton.Toggle();
            Assert.Equal(1, changeCount);
            Assert.True(threeStateButton.IsChecked);

            threeStateButton.Toggle();
            Assert.Equal(2, changeCount);
            Assert.Null(threeStateButton.IsChecked);

            threeStateButton.Toggle();
            Assert.Equal(3, changeCount);
            Assert.False(threeStateButton.IsChecked);
        }

        private class Class1 : NotifyingBase
        {
            private bool _foo;
            private bool? nullableFoo;

            public bool Foo
            {
                get { return _foo; }
                set { _foo = value; RaisePropertyChanged(); }
            }

            public bool? NullableFoo
            {
                get { return nullableFoo; }
                set { nullableFoo = value; RaisePropertyChanged(); }
            }
        }

        private class TestToggleButton : ToggleButton
        {
            public new void Toggle() => base.Toggle();
        }
    }
}
