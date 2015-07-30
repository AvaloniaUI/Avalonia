// -----------------------------------------------------------------------
// <copyright file="TemplatedControlTests.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.UnitTests.Primitives
{
    using System;
    using System.Linq;
    using Collections;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.LogicalTree;
    using Perspex.VisualTree;
    using Xunit;

    public class TemplatedControlTests
    {
        [Fact]
        public void ApplyTemplate_Should_Create_Visual_Child()
        {
            var target = new TemplatedControl
            {
                Template = new ControlTemplate(_ => new Decorator
                {
                    Child = new Panel
                    {
                        Children = new Controls
                        {
                            new TextBlock(),
                            new Border(),
                        }
                    }
                }),
            };

            target.ApplyTemplate();

            var types = target.GetVisualDescendents().Select(x => x.GetType()).ToList();

            Assert.Equal(
                new[]
                {
                    typeof(Decorator),
                    typeof(Panel),
                    typeof(TextBlock),
                    typeof(Border)
                },
                types);
            Assert.Empty(target.GetLogicalChildren());
        }

        [Fact]
        public void Templated_Children_Should_Have_TemplatedParent_Set()
        {
            var target = new TemplatedControl
            {
                Template = new ControlTemplate(_ => new Decorator
                {
                    Child = new Panel
                    {
                        Children = new Controls
                        {
                            new TextBlock(),
                            new Border(),
                        }
                    }
                }),
            };

            target.ApplyTemplate();

            var templatedParents = target.GetVisualDescendents()
                .OfType<IControl>()
                .Select(x => x.TemplatedParent)
                .ToList();

            Assert.Equal(4, templatedParents.Count);
            Assert.True(templatedParents.All(x => x == target));
        }

        [Fact]
        public void Nested_TemplatedControls_Should_Be_Expanded_And_Have_Correct_TemplatedParent()
        {
            var target = new ItemsControl
            {
                Template = new ControlTemplate<ItemsControl>(ItemsControlTemplate),
                Items = new[] { "Foo", }
            };

            target.ApplyTemplate();

            var scrollViewer = target.GetVisualDescendents()
                .OfType<ScrollViewer>()
                .Single();
            var types = target.GetVisualDescendents()
                .Select(x => x.GetType())
                .ToList();
            var templatedParents = target.GetVisualDescendents()
                .OfType<IControl>()
                .Select(x => x.TemplatedParent)
                .ToList();

            Assert.Equal(
                new[]
                {
                    typeof(Border),
                    typeof(ScrollViewer),
                    typeof(ScrollContentPresenter),
                    typeof(ItemsPresenter),
                    typeof(StackPanel),
                    typeof(TextBlock),
                },
                types);

            Assert.Equal(
                new object[]
                {
                    target,
                    target,
                    scrollViewer,
                    target,
                    target,
                    null
                },
                templatedParents);
        }

        private static IControl ItemsControlTemplate(ItemsControl control)
        {
            return new Border
            {
                Child = new ScrollViewer
                {
                    Template = new ControlTemplate<ScrollViewer>(ScrollViewerTemplate),
                    Content = new ItemsPresenter
                    {
                        Name = "itemsPresenter",
                        [~ItemsPresenter.ItemsProperty] = control[~ListBox.ItemsProperty],
                        [~ItemsPresenter.ItemsPanelProperty] = control[~ListBox.ItemsPanelProperty],
                    }
                }
            };
        }

        private static Control ScrollViewerTemplate(ScrollViewer control)
        {
            var result = new ScrollContentPresenter
            {
                Name = "contentPresenter",
                [~ScrollContentPresenter.ContentProperty] = control[~ScrollViewer.ContentProperty],
            };

            return result;
        }
    }
}