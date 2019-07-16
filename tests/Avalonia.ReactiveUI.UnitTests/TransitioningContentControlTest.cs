// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using ReactiveUI;
using Splat;
using Xunit;

namespace Avalonia.ReactiveUI.UnitTests
{
    public class TransitioningContentControlTest
    {
        [Fact]
        public void Transitioning_Control_Should_Derive_Template_From_Content_Control()
        {
            var target = new TransitioningContentControl();
            var stylable = (IStyledElement)target;
            Assert.Equal(typeof(ContentControl),stylable.StyleKey);
        }

        [Fact]
        public void Transitioning_Control_Template_Should_Be_Instantiated() 
        {
            var target = new TransitioningContentControl
            {
                PageTransition = null,
                Template = GetTemplate(),
                Content = "Foo"
            };
            target.ApplyTemplate();
            ((ContentPresenter)target.Presenter).UpdateChild();

            var child = ((IVisual)target).VisualChildren.Single();
            Assert.IsType<Border>(child);
            child = child.VisualChildren.Single();
            Assert.IsType<ContentPresenter>(child);
            child = child.VisualChildren.Single();
            Assert.IsType<TextBlock>(child);
        }

        private FuncControlTemplate GetTemplate()
        {
            return new FuncControlTemplate<ContentControl>((parent, scope) =>
            {
                return new Border
                {
                    Background = new Media.SolidColorBrush(0xffffffff),
                    Child = new ContentPresenter
                    {
                        Name = "PART_ContentPresenter",
                        [~ContentPresenter.ContentProperty] = parent[~ContentControl.ContentProperty],
                        [~ContentPresenter.ContentTemplateProperty] = parent[~ContentControl.ContentTemplateProperty],
                    }.RegisterInNameScope(scope)
                };
            });
        }
    }
}
