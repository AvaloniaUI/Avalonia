using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Xunit;

namespace Avalonia.Controls.UnitTests.Primitives
{
    public class TabStripTests
    {
        [Fact]
        public void First_Tab_Should_Not_Be_Selected_By_Default()
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

            Assert.Equal(-1, target.SelectedIndex);
            Assert.Same(null, target.SelectedItem);
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

        private Control CreateTabStripTemplate(TabStrip parent, INameScope scope)
        {
            return new ItemsPresenter
            {
                Name = "itemsPresenter",
            }.RegisterInNameScope(scope);
        }
    }
}
