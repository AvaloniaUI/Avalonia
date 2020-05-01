using Avalonia.Controls.Mixins;
using Avalonia.Controls.Primitives;
using Xunit;

namespace Avalonia.Controls.UnitTests.Mixins
{
    public class SelectableMixinTests
    {
        [Fact]
        public void Selected_Class_Should_Not_Initially_Be_Added()
        {
            var target = new TestControl();

            Assert.Empty(target.Classes);
        }

        [Fact]
        public void Setting_IsSelected_Should_Add_Selected_Class()
        {
            var target = new TestControl();

            target.IsSelected = true;

            Assert.Equal(new[] { ":selected" }, target.Classes);
        }

        [Fact]
        public void Clearing_IsSelected_Should_Remove_Selected_Class()
        {
            var target = new TestControl();

            target.IsSelected = true;
            target.IsSelected = false;

            Assert.Empty(target.Classes);
        }

        [Fact]
        public void Setting_IsSelected_Should_Raise_IsSelectedChangedEvent()
        {
            var target = new TestControl();
            var raised = false;

            target.AddHandler(
                SelectingItemsControl.IsSelectedChangedEvent,
                (s, e) => raised = true);

            target.IsSelected = true;

            Assert.True(raised);
        }

        private class TestControl : Control, ISelectable
        {
            public static readonly StyledProperty<bool> IsSelectedProperty =
                AvaloniaProperty.Register<TestControl, bool>(nameof(IsSelected));

            static TestControl()
            {
                SelectableMixin.Attach<TestControl>(IsSelectedProperty);
            }

            public bool IsSelected
            {
                get { return GetValue(IsSelectedProperty); }
                set { SetValue(IsSelectedProperty, value); }
            }
        }
    }
}
