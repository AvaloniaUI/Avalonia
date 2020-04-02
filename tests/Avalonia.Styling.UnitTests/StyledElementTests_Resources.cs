using System;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class StyledElementTests_Resources
    {
        [Fact]
        public void FindResource_Should_Find_Control_Resource()
        {
            var target = new StyledElement
            {
                Resources =
                {
                    { "foo", "foo-value" },
                }
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Control_Resource_In_Parent()
        {
            Control target;

            var root = new Decorator
            {
                Resources =
                {
                    { "foo", "foo-value" },
                },
                Child = target = new Control(),
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Application_Resource()
        {
            Control target;

            var app = new Application
            {
                Resources =
                {
                    { "foo", "foo-value" },
                },
            };

            var root = new TestRoot
            {
                Child = target = new Control(),
                StylingParent = app,
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Style_Resource()
        {
            var target = new StyledElement
            {
                Styles =
                {
                    new Style
                    {
                        Resources =
                        {
                            { "foo", "foo-value" },
                        }
                    }
                },
                Resources =
                {
                    { "bar", "bar-value" },
                },
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Styles_Resource()
        {
            var target = new StyledElement
            {
                Styles =
                {
                    new Styles
                    {
                        Resources =
                        {
                            { "foo", "foo-value" },
                        }
                    }
                },
                Resources =
                {
                    { "bar", "bar-value" },
                },
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void FindResource_Should_Find_Application_Style_Resource()
        {
            Control target;

            var app = new Application
            {
                Styles =
                {
                    new Style
                    {
                        Resources =
                        {
                            { "foo", "foo-value" },
                        },
                    }
                },
                Resources =
                {
                    { "bar", "bar-value" },
                },
            };

            var root = new TestRoot
            {
                Child = target = new Control(),
                StylingParent = app,
            };

            Assert.Equal("foo-value", target.FindResource("foo"));
        }

        [Fact]
        public void Adding_Resource_Should_Call_Raise_ResourceChanged_On_Logical_Children()
        {
            Border child;

            var target = new ContentControl
            {
                Content = child = new Border(),
                Template = ContentControlTemplate(),
            };

            var raisedOnTarget = false;
            var raisedOnPresenter = false;
            var raisedOnChild = false;

            target.Measure(Size.Infinity);
            target.ResourcesChanged += (_, __) => raisedOnTarget = true;
            target.Presenter.ResourcesChanged += (_, __) => raisedOnPresenter = true;
            child.ResourcesChanged += (_, __) => raisedOnChild = true;

            target.Resources.Add("foo", "bar");

            Assert.True(raisedOnTarget);
            Assert.True(raisedOnPresenter);
            Assert.True(raisedOnChild);
        }

        [Fact]
        public void Adding_Resource_To_Styles_Should_Raise_ResourceChanged()
        {
            var target = new Decorator();
            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            target.Styles.Resources.Add("foo", "bar");

            Assert.True(raised);
        }

        [Fact]
        public void Adding_Resource_To_Nested_Style_Should_Raise_ResourceChanged()
        {
            Style style;
            var target = new StyledElement
            {
                Styles =
                {
                    (style = new Style()),
                }
            };

            var raised = false;

            target.ResourcesChanged += (_, __) => raised = true;
            style.Resources.Add("foo", "bar");

            Assert.True(raised);
        }

        [Fact]
        public void Setting_Logical_Parent_Raises_Child_ResourcesChanged()
        {
            var parent = new ContentControl();
            var child = new StyledElement();

            ((ISetLogicalParent)child).SetParent(parent);
            var raisedOnChild = false;

            child.ResourcesChanged += (_, __) => raisedOnChild = true;

            parent.Resources.Add("foo", "bar");

            Assert.True(raisedOnChild);
        }

        [Fact]
        public void Setting_Logical_Parent_Raises_Style_ResourcesChanged()
        {
            var style = new Style(x => x.OfType<Canvas>());
            var parent = new ContentControl();
            var child = new StyledElement { Styles = { style } };

            ((ISetLogicalParent)child).SetParent(parent);
            var raised = false;

            style.ResourcesChanged += (_, __) => raised = true;

            parent.Resources.Add("foo", "bar");

            Assert.True(raised);
        }

        private IControlTemplate ContentControlTemplate()
        {
            return new FuncControlTemplate<ContentControl>((x, scope) =>
                new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                }.RegisterInNameScope(scope));
        }
    }
}
