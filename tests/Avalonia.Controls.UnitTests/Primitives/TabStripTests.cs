using System.Collections.ObjectModel;
using System.Linq;
using Moq;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TabStripTests
    {
        [Fact]
        public void First_Tab_Should_Be_Selected_By_Default()
        {
            var target = new TabStrip
            {
                Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items =
                {
                    new TabItem
                    {
                        Name = "first"
                    },
                    new TabItem
                    {
                        Name = "second"
                    },
                }
            };

            target.ApplyTemplate();

            Assert.Equal(0, target.SelectedIndex);
            Assert.Same(target.Items[0], target.SelectedItem);
        }

        [Fact]
        public void Setting_SelectedItem_Should_Set_Selection()
        {
            var target = new TabStrip
            {
                Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items =
                {
                    new TabItem
                    {
                        Name = "first"
                    },
                    new TabItem
                    {
                        Name = "second"
                    },
                },             
            };

            target.SelectedItem = target.Items[1];
            target.ApplyTemplate();

            Assert.Equal(1, target.SelectedIndex);
            Assert.Same(target.Items[1], target.SelectedItem);
        }

        [Fact]
        public void Removing_Selected_Should_Select_First()
        {
            var target = new TabStrip
            {
                Template = new FuncControlTemplate<TabStrip>(CreateTabStripTemplate),
                Items =
                {
                    new TabItem
                    {
                        Name = "first"
                    },
                    new TabItem
                    {
                        Name = "second"
                    },
                    new TabItem
                    {
                        Name = "3rd"
                    },
                }
            };

            target.ApplyTemplate();
            target.SelectedItem = target.Items[1];
            Assert.Same(target.Items[1], target.SelectedItem);
            target.Items.RemoveAt(1);

            Assert.Equal(0, target.SelectedIndex);
            Assert.Same(target.Items[0], target.SelectedItem);
            Assert.Same("first", ((TabItem)target.SelectedItem).Name);
        }

        private Control CreateTabStripTemplate(TabStrip parent, INameScope scope)
        {
            return new ItemsPresenter
            {
                Name = "itemsPresenter",
            }.RegisterInNameScope(scope);
        }
    }
}
