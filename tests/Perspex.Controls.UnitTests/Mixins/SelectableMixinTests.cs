





namespace Perspex.Controls.UnitTests.Mixins
{
    using Perspex.Controls.Mixins;
    using Perspex.Controls.Primitives;
    using Xunit;

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

            Assert.Equal(new[] { "selected" }, target.Classes);
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
            public static readonly PerspexProperty<bool> IsSelectedProperty =
                PerspexProperty.Register<TestControl, bool>(nameof(IsSelected));

            static TestControl()
            {
                SelectableMixin.Attach<TestControl>(IsSelectedProperty);
            }

            public bool IsSelected
            {
                get { return this.GetValue(IsSelectedProperty); }
                set { this.SetValue(IsSelectedProperty, value); }
            }
        }
    }
}
