using System.Linq;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.VisualTree;
using Xunit;

#nullable enable

namespace Avalonia.Controls.UnitTests.Automation
{
    public class ControlAutomationPeerTests
    {
        public class Children
        {
            [Fact]
            public void Creates_Children_For_Controls_In_Visual_Tree()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var target = CreatePeer(panel);

                Assert.Equal(
                    panel.GetVisualChildren(),
                    target.GetChildren().Cast<ControlAutomationPeer>().Select(x => x.Owner));
            }

            [Fact]
            public void Creates_Children_when_Controls_Attached_To_Visual_Tree()
            {
                var contentControl = new ContentControl
                {
                    Template = new FuncControlTemplate<ContentControl>((o, _) =>
                        new ContentPresenter
                        {
                            Name = "PART_ContentPresenter",
                            [!ContentPresenter.ContentProperty] = o[!ContentControl.ContentProperty],
                        }),
                    Content = new Border(),
                };

                var target = CreatePeer(contentControl);

                Assert.Empty(target.GetChildren());

                contentControl.Measure(Size.Infinity);

                Assert.Equal(1, target.GetChildren().Count);
            }

            [Fact]
            public void Updates_Children_When_VisualChildren_Added()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var target = CreatePeer(panel);
                var children = target.GetChildren();

                Assert.Equal(2, children.Count);

                panel.Children.Add(new Decorator());

                children = target.GetChildren();
                Assert.Equal(3, children.Count);
            }

            [Fact]
            public void Updates_Children_When_VisualChildren_Removed()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var target = CreatePeer(panel);
                var children = target.GetChildren();

                Assert.Equal(2, children.Count);

                panel.Children.RemoveAt(1);

                children = target.GetChildren();
                Assert.Equal(1, children.Count);
            }

            [Fact]
            public void Updates_Children_When_Visibility_Changes_From_Visible_To_Invisible()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border(),
                    },
                };

                var target = CreatePeer(panel);
                var children = target.GetChildren();

                Assert.Equal(2, children.Count);

                panel.Children[1].IsVisible = false;
                children = target.GetChildren();
                Assert.Equal(1, children.Count);

                panel.Children[1].IsVisible = true;
                children = target.GetChildren();
                Assert.Equal(2, children.Count);
            }

            [Fact]
            public void Updates_Children_When_Visibility_Changes_From_Invisible_To_Visible()
            {
                var panel = new Panel
                {
                    Children =
                    {
                        new Border(),
                        new Border { IsVisible = false },
                    },
                };

                var target = CreatePeer(panel);
                var children = target.GetChildren();
                Assert.Equal(1, children.Count);

                panel.Children[1].IsVisible = true;
                children = target.GetChildren();
                Assert.Equal(2, children.Count);
            }
        }

        public class Parent
        {
            [Fact]
            public void Connects_Peer_To_Tree_When_GetParent_Called()
            {
                var border = new Border();
                var tree = new Decorator
                {
                    Child = new Decorator
                    {
                        Child = border,
                    }
                };

                // We're accessing Border directly without going via its ancestors. Because the tree
                // is built lazily, ensure that calling GetParent causes the ancestor tree to be built.
                var target = CreatePeer(border);

                var parentPeer = Assert.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
                Assert.Same(border.GetVisualParent(), parentPeer.Owner);
            }

            [Fact]
            public void Parent_Updated_When_Moved_To_Separate_Visual_Tree()
            {
                var border = new Border();
                var root1 = new Decorator { Child = border };
                var root2 = new Decorator();
                var target = CreatePeer(border);

                var parentPeer = Assert.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
                Assert.Same(root1, parentPeer.Owner);

                root1.Child = null;

                Assert.Null(target.GetParent());

                root2.Child = border;

                parentPeer = Assert.IsAssignableFrom<ControlAutomationPeer>(target.GetParent());
                Assert.Same(root2, parentPeer.Owner);
            }
        }

        private static AutomationPeer CreatePeer(Control control)
        {
            return ControlAutomationPeer.CreatePeerForElement(control);
        }
    }
}
