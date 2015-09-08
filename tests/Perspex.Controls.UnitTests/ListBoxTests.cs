// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.LogicalTree;
using Perspex.Styling;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ListBoxTests
    {
        [Fact]
        public void LogicalChildren_Should_Be_Set()
        {
            var target = new ListBox
            {
                Template = new ControlTemplate(this.CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            target.ApplyTemplate();

            Assert.Equal(3, target.GetLogicalChildren().Count());

            foreach (var child in target.GetLogicalChildren())
            {
                Assert.IsType<ListBoxItem>(child);
            }
        }

        [Fact]
        public void Setting_Item_IsSelected_Sets_ListBox_Selection()
        {
            var target = new ListBox
            {
                Template = new ControlTemplate(this.CreateListBoxTemplate),
                Items = new[] { "Foo", "Bar", "Baz " },
            };

            target.ApplyTemplate();

            ((ListBoxItem)target.GetLogicalChildren().ElementAt(1)).IsSelected = true;

            Assert.Equal("Bar", target.SelectedItem);
            Assert.Equal(1, target.SelectedIndex);
        }

        private Control CreateListBoxTemplate(ITemplatedControl parent)
        {
            return new ScrollViewer
            {
                Template = new ControlTemplate(this.CreateScrollViewerTemplate),
                Content = new ItemsPresenter
                {
                    Name = "itemsPresenter",
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
