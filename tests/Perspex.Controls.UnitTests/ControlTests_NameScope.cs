// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls.Presenters;
using Perspex.Controls.Templates;
using Perspex.Rendering;
using Xunit;

namespace Perspex.Controls.UnitTests
{
    public class ControlTests_NameScope
    {
        [Fact]
        public void Controls_Should_Register_With_NameScope()
        {
            var root = new TestRoot
            {
                Content = new Border
                {
                    Name = "foo",
                    Child = new Border
                    {
                        Name = "bar",
                    }
                }
            };

            root.ApplyTemplate();

            Assert.Same(root.Find("foo"), root.Content);
            Assert.Same(root.Find("bar"), ((Border)root.Content).Child);
        }

        [Fact]
        public void Control_Should_Unregister_With_NameScope()
        {
            var root = new TestRoot
            {
                Content = new Border
                {
                    Name = "foo",
                    Child = new Border
                    {
                        Name = "bar",
                    }
                }
            };

            root.ApplyTemplate();
            root.Content = null;
            root.Presenter.ApplyTemplate();

            Assert.Null(root.Find("foo"));
            Assert.Null(root.Find("bar"));
        }

        [Fact]
        public void Control_Should_Not_Register_With_Template_NameScope()
        {
            var root = new TestRoot
            {
                Content = new Border
                {
                    Name = "foo",
                }
            };

            root.ApplyTemplate();

            Assert.Null(NameScope.GetNameScope(root.Presenter).Find("foo"));
        }

        private class TestRoot : ContentControl, IRenderRoot, INameScope
        {
            private readonly NameScope _nameScope = new NameScope();

            public TestRoot()
            {
                Template = new FuncControlTemplate<TestRoot>(x => new ContentPresenter
                {
                    Name = "PART_ContentPresenter",
                    [!ContentPresenter.ContentProperty] = x[!ContentControl.ContentProperty],
                });
            }

            public event EventHandler<NameScopeEventArgs> Registered
            {
                add { _nameScope.Registered += value; }
                remove { _nameScope.Registered -= value; }
            }

            public event EventHandler<NameScopeEventArgs> Unregistered
            {
                add { _nameScope.Unregistered += value; }
                remove { _nameScope.Unregistered -= value; }
            }

            public IRenderQueueManager RenderQueueManager
            {
                get { throw new NotImplementedException(); }
            }

            public Point TranslatePointToScreen(Point p)
            {
                throw new NotImplementedException();
            }

            public void Register(string name, object element)
            {
                _nameScope.Register(name, element);
            }

            public object Find(string name)
            {
                return _nameScope.Find(name);
            }

            public void Unregister(string name)
            {
                _nameScope.Unregister(name);
            }
        }
    }
}
