// -----------------------------------------------------------------------
// <copyright file="ItemsPresenterTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Presenters
{
    using Perspex.Controls.Presenters;
    using Perspex.Input;
    using Perspex.VisualTree;
    using System.Linq;
    using Xunit;

    public class ItemsPresenterTests
    {
        [Fact]
        public void Panel_Should_Be_Created_From_ItemsPanel_Template()
        {
            var panel = new Panel();
            var target = new ItemsPresenter
            {
                ItemsPanel = new ItemsPanelTemplate(() => panel),
            };

            target.ApplyTemplate();

            Assert.Equal(panel, target.Panel);
        }

        [Fact]
        public void Panel_TemplatedParent_Should_Be_Set()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            Assert.Equal(target, target.Panel.TemplatedParent);
        }

        [Fact]
        public void Panel_TabNavigation_Should_Be_Set_To_Once()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            Assert.Equal(KeyboardNavigationMode.Once, KeyboardNavigation.GetTabNavigation(target.Panel));
        }

        [Fact]
        public void Panel_Should_Be_Visual_Child()
        {
            var target = new ItemsPresenter();

            target.ApplyTemplate();

            var child = target.GetVisualChildren().Single();

            Assert.Equal(target.Panel, child);
        }

        [Fact]
        public void Items_Should_Be_Created_On_ApplyTemplate()
        {
            var target = new ItemsPresenter
            {
                Items = new[] { "foo", "bar" },
            };

            target.ApplyTemplate();

            Assert.Equal(2, target.Panel.GetVisualChildren().Count());
        }
    }
}
