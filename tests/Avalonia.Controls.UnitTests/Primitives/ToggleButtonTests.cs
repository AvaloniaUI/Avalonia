using System;
using Avalonia.Controls.UnitTests.Utils;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;
using MouseButton = Avalonia.Input.MouseButton;

namespace Avalonia.Controls.Primitives.UnitTests
{
    public class ToggleButtonTests : ScopedTestBase
    {
        private const string uncheckedClass = ":unchecked";
        private const string checkedClass = ":checked";
        private const string indeterminateClass = ":indeterminate";
        private readonly MouseTestHelper _mouse = new();

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

        [Fact]
        public void ToggleButton_Does_Not_Toggle_When_Command_Becomes_Disabled_Between_Press_And_Release()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var command = new TestCommand(true);
            var target = CreateToggleButton(command);
            var root = CreateRoot(target);
            var point = new Point(50, 50);

            RaisePointerEntered(target);
            RaisePointerMove(target, point);
            RaisePointerPressed(target, 1, MouseButton.Left, point);

            Assert.True(target.IsPressed);
            Assert.False(target.IsChecked);

            command.IsEnabled = false;

            Assert.False(target.IsEffectivelyEnabled);

            RaisePointerReleased(target, MouseButton.Left, point);

            Assert.False(target.IsChecked);
        }

        [Fact]
        public void ToggleButton_Toggles_When_Command_Remains_Executable_Through_Release()
        {
            using var app = UnitTestApplication.Start(TestServices.StyledWindow);

            var target = CreateToggleButton(new TestCommand(true));
            var root = CreateRoot(target);
            var point = new Point(50, 50);

            RaisePointerEntered(target);
            RaisePointerMove(target, point);
            RaisePointerPressed(target, 1, MouseButton.Left, point);

            Assert.True(target.IsPressed);
            Assert.True(target.IsEffectivelyEnabled);

            RaisePointerReleased(target, MouseButton.Left, point);

            Assert.True(target.IsChecked);
        }

        private Window CreateRoot(ToggleButton target)
        {
            var renderer = new Mock<IHitTester>();

            renderer
                .Setup(r => r.HitTest(It.IsAny<Point>(), It.IsAny<Visual>(), It.IsAny<Func<Visual, bool>>()))
                .Returns<Point, Visual, Func<Visual, bool>>((point, root, filter) =>
                    root.Bounds.Contains(point) ? new Visual[] { root } : Array.Empty<Visual>());

            var root = new Window { HitTesterOverride = renderer.Object, Content = target };
            root.Show();
            return root;
        }

        private ToggleButton CreateToggleButton(TestCommand command)
        {
            return new ToggleButton
            {
                Width = 100,
                Height = 100,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Command = command,
            };
        }

        private void RaisePointerPressed(ToggleButton button, int clickCount, MouseButton mouseButton, Point position)
        {
            _mouse.Down(button, mouseButton, position, clickCount: clickCount);
        }

        private void RaisePointerReleased(ToggleButton button, MouseButton mouseButton, Point position)
        {
            _mouse.Up(button, mouseButton, position);
        }

        private void RaisePointerEntered(ToggleButton button)
        {
            _mouse.Enter(button);
        }

        private void RaisePointerMove(ToggleButton button, Point position)
        {
            _mouse.Move(button, position);
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
