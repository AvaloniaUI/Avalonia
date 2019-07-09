// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class HeaderedItemsControlTests
    {
        [Fact]
        public void Control_Header_Should_Be_Logical_Child_Before_ApplyTemplate()
        {
            var target = new HeaderedItemsControl
            {
                Template = GetTemplate(),
            };

            var child = new Control();
            target.Header = child;

            Assert.Equal(child.Parent, target);
            Assert.Equal(child.GetLogicalParent(), target);
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void DataTemplate_Created_Control_Should_Be_Logical_Child_After_ApplyTemplate()
        {
            var target = new HeaderedItemsControl
            {
                Template = GetTemplate(),
            };

            target.Header = "Foo";
            target.ApplyTemplate();
            ((ContentPresenter)target.HeaderPresenter).UpdateChild();

            var child = target.HeaderPresenter.Child;

            Assert.NotNull(child);
            Assert.Equal(target, child.Parent);
            Assert.Equal(target, child.GetLogicalParent());
            Assert.Equal(new[] { child }, target.GetLogicalChildren());
        }

        [Fact]
        public void Clearing_Content_Should_Clear_Logical_Child()
        {
            var target = new HeaderedItemsControl();
            var child = new Control();

            target.Header = child;
            target.Header = null;

            Assert.Null(child.Parent);
            Assert.Null(child.GetLogicalParent());
            Assert.Empty(target.GetLogicalChildren());
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<HeaderedItemsControl>((parent, scope) =>
            {
                return new Border
                {
                    Child = new ContentPresenter
                    {
                        Name = "PART_HeaderPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~HeaderedItemsControl.HeaderProperty],
                    }.RegisterInNameScope(scope)
                };
            });
        }
    }
}
