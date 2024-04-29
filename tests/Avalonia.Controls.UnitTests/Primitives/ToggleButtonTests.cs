using Avalonia.Data;
using Avalonia.UnitTests;
using Xunit;

#pragma warning disable CS0618 // Type or member is obsolete -- we're testing these members

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
        public void ToggleButton_Events_Are_Raised_On_Is_Checked_Changes()
        {
            var threeStateButton = new ToggleButton();

            bool checkedRaised = false;
            threeStateButton.Checked += (_, __) => checkedRaised = true;

            threeStateButton.IsChecked = true;
            Assert.True(checkedRaised);

            bool uncheckedRaised = false;
            threeStateButton.Unchecked += (_, __) => uncheckedRaised = true;

            threeStateButton.IsChecked = false;
            Assert.True(uncheckedRaised);

            bool indeterminateRaised = false;
            threeStateButton.Indeterminate += (_, __) => indeterminateRaised = true;

            threeStateButton.IsChecked = null;
            Assert.True(indeterminateRaised);
        }

        [Fact]
        public void ToggleButton_Events_Are_Raised_When_Toggling()
        {
            var threeStateButton = new TestToggleButton { IsThreeState = true };

            bool checkedRaised = false;
            threeStateButton.Checked += (_, __) => checkedRaised = true;

            threeStateButton.Toggle();
            Assert.True(checkedRaised);

            bool indeterminateRaised = false;
            threeStateButton.Indeterminate += (_, __) => indeterminateRaised = true;

            threeStateButton.Toggle();
            Assert.True(indeterminateRaised);

            bool uncheckedRaised = false;
            threeStateButton.Unchecked += (_, __) => uncheckedRaised = true;

            threeStateButton.Toggle();
            Assert.True(uncheckedRaised);
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
