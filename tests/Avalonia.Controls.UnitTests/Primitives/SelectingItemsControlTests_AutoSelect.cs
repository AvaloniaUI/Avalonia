using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Collections;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class SelectingItemsControlTests_AutoSelect
    {
        [Fact]
        public void First_Item_Should_Be_Selected()
        {
            var target = new TestSelector
            {
                ItemsSource = new[] { "foo", "bar" },
                Template = Template(),
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void First_Item_Should_Be_Selected_When_Added()
        {
            var items = new AvaloniaList<string>();
            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }


        [Fact]
        public void First_Item_Should_Be_Selected_When_Reset()
        {
            var items = new ResetOnAdd();
            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            items.Add("foo");

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Item_Should_Be_Selected_When_Selection_Removed()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar", "baz", "qux" });

            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 2;
            items.RemoveAt(2);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("foo", target.SelectedItem);
        }

        [Fact]
        public void Selection_Should_Be_Cleared_When_No_Items_Left()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar" });

            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.SelectedIndex = 1;
            items.RemoveAt(1);
            items.RemoveAt(0);

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Null(target.SelectedItem);
        }

        [Fact]
        public void Removing_Selected_First_Item_Should_Select_Next_Item()
        {
            var items = new AvaloniaList<string>(new[] { "foo", "bar" });
            var target = new TestSelector
            {
                ItemsSource = items,
                Template = Template(),
            };

            target.ApplyTemplate();
            target.Presenter.ApplyTemplate();
            items.RemoveAt(0);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Equal("bar", target.SelectedItem);
        }

        private static FuncControlTemplate Template()
        {
            return new FuncControlTemplate<SelectingItemsControl>((control, scope) =>
                new ItemsPresenter
                {
                    Name = "itemsPresenter",
                    [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                }.RegisterInNameScope(scope));
        }

        private class TestSelector : SelectingItemsControl
        {
            static TestSelector()
            {
                SelectionModeProperty.OverrideDefaultValue<TestSelector>(SelectionMode.AlwaysSelected);
            }
        }

        private class ResetOnAdd : List<string>, INotifyCollectionChanged
        {
            public event NotifyCollectionChangedEventHandler CollectionChanged;

            public new void Add(string item)
            {
                base.Add(item);
                CollectionChanged?.Invoke(
                    this,
                    new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }
    }
}
