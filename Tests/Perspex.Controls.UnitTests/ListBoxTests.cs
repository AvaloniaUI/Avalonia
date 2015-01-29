// -----------------------------------------------------------------------
// <copyright file="ItemsControlTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests
{
    using System;
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.LogicalTree;
    using Perspex.Styling;
    using Xunit;

    public class ListBoxTests
    {
        [Fact]
        public void LogicalChildren_Should_Be_Set()
        {
            var target = new ListBox
            {
                Template = new ControlTemplate(x => this.CreateListBoxTemplate(x)),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            target.ApplyTemplate();

            Assert.Equal(3, target.GetLogicalChildren().Count());

            foreach (var child in target.GetLogicalChildren())
            {
                Assert.IsType<ListBoxItem>(child);
            }
        }

        private Control CreateListBoxTemplate(ITemplatedControl parent)
        {
            return new ScrollViewer
            {
                Template = new ControlTemplate(x => this.CreateScrollViewerTemplate(x)),
                Content = new ItemsPresenter
                {
                    Id = "itemsPresenter",
                    [~ItemsPresenter.ItemsProperty] = parent.GetObservable(ListBox.ItemsProperty),
                }
            };
        }

        private Control CreateScrollViewerTemplate(ITemplatedControl parent)
        {
            return new ScrollContentPresenter
            {
                [~ScrollContentPresenter.ContentProperty] = parent.GetObservable(ScrollViewer.ContentProperty),
            };
        }
    }
}
