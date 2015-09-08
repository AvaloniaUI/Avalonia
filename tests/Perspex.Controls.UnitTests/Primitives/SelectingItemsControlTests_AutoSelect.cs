





namespace Perspex.Controls.UnitTests.Primitives
{
    using Perspex.Collections;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Interactivity;
    using Xunit;

    public class SelectingItemsControlTests_AutoSelect
    {
        [Fact]
        public void First_Item_Should_Be_Selected()
        {
            var target = new SelectingItemsControl
            {
                AutoSelect = true,
                Items = new[] { "foo", "bar" },
                Template = this.Template(),
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void First_Item_Should_Be_Selected_When_Added()
        {
            var items = new PerspexList<string>();
            var target = new SelectingItemsControl
            {
                AutoSelect = true,
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Item_Should_Be_Selected_When_Selection_Removed()
        {
            var items = new PerspexList<string>(new[] { "foo", "bar", "baz", "qux" });

            var target = new SelectingItemsControl
            {
                AutoSelect = true,
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;
            items.RemoveAt(2);

            Assert.Equal(2, target.SelectedIndex);
            Assert.Equal("qux", target.SelectedItem);
        }

        [Fact]
        public void Selection_Should_Be_Cleared_When_No_Items_Left()
        {
            var items = new PerspexList<string>(new[] { "foo", "bar" });

            var target = new SelectingItemsControl
            {
                AutoSelect = true,
                Items = items,
                Template = this.Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;
            items.RemoveAt(1);
            items.RemoveAt(0);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        private ControlTemplate Template()
        {
            return new ControlTemplate<SelectingItemsControl>(control =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = control[~ListBox.ItemsProperty],
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ListBox.ItemsPanelProperty],
                });
        }

        private class Item : Control, ISelectable
        {
            public bool IsSelected { get; set; }
        }
    }
}
