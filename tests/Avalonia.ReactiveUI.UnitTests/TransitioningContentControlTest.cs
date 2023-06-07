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
        public void Transitioning_Control_Template_Should_Be_Instantiated() 
        {
            var target = new TransitioningContentControl
            {
                PageTransition = null,
                Template = GetTemplate(),
                Content = "Foo"
            };
            target.ApplyTemplate();
            target.Presenter.UpdateChild();

            var child = ((Visual)target).GetVisualChildren().Single();
            Assert.IsType<Border>(child);
            child = child.GetVisualChildren().Single();
            Assert.IsType<ContentPresenter>(child);
            child = child.GetVisualChildren().Single();
            Assert.IsType<TextBlock>(child);
        }

        private static FuncControlTemplate GetTemplate()
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
