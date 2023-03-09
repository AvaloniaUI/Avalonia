using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    /// <summary>
    /// Tests for ContentControls that are not attached to a logical tree.
    /// </summary>
    public class ContentPresenterTests_Unrooted
    {
        [Fact]
        public void Setting_Content_To_Control_Should_Not_Set_Child_Unless_UpdateChild_Called()
        {
            var target = new ContentPresenter();
            var child = new Border();

            target.Content = child;
            Assert.Null(target.Child);

            target.ApplyTemplate();
            Assert.Null(target.Child);

            target.UpdateChild();
            Assert.Equal(child, target.Child);
        }

        [Fact]
        public void Setting_Content_To_String_Should_Not_Create_TextBlock_Unless_UpdateChild_Called()
        {
            var target = new ContentPresenter();

            target.Content = "Foo";
            Assert.Null(target.Child);

            target.ApplyTemplate();
            Assert.Null(target.Child);

            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);
            Assert.Equal("Foo", ((TextBlock)target.Child).Text);
        }

        [Fact]
        public void Clearing_Control_Content_Should_Remove_Child_Immediately()
        {
            var target = new ContentPresenter();
            var child = new Border();

            target.Content = child;
            target.UpdateChild();
            Assert.Equal(child, target.Child);

            target.Content = null;
            Assert.Null(target.Child);
        }

        [Fact]
        public void Clearing_String_Content_Should_Remove_Child_Immediately()
        {
            var target = new ContentPresenter();

            target.Content = "Foo";
            target.UpdateChild();
            Assert.IsType<TextBlock>(target.Child);

            target.Content = null;
            Assert.Null(target.Child);
        }

        [Fact]
        public void Adding_To_Logical_Tree_Should_Reevaluate_DataTemplates()
        {
            var root = new TestRoot();
            var target = new ContentPresenter();

            target.Content = "Foo";
            Assert.Null(target.Child);

            root.Child = target;
            target.ApplyTemplate();
            Assert.IsType<TextBlock>(target.Child);

            root.Child = null;
            root = new TestRoot
            {
                DataTemplates =
                {
                    new FuncDataTemplate<string>((x, _) => new Decorator()),
                },
            };

            root.Child = target;
            target.ApplyTemplate();
            Assert.IsType<Decorator>(target.Child);
        }

        [Fact]
        public void Should_Reset_InheritanceParent_When_Child_Removed()
        {
            var logicalParent = new Canvas();
            var child = new TextBlock();
            var target = new ContentPresenter();

            ((ISetLogicalParent)child).SetParent(logicalParent);
            target.Content = child;
            target.UpdateChild();
            target.Content = null;
            target.UpdateChild();

            // InheritanceParent is exposed via StylingParent.
            Assert.Same(logicalParent, ((IStyleHost)child).StylingParent);
        }
    }
}
