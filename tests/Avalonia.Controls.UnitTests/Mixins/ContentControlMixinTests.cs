// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using Avalonia.Collections;
using Avalonia.Controls.Mixins;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests.Mixins
{
    public class ContentControlMixinTests
    {
        [Fact]
        public void Multiple_Mixin_Usages_Should_Not_Throw()
        {
            var target = new TestControl()
            {
                Template = new FuncControlTemplate(_ => new Panel
                {
                    Children =
                    {
                        new ContentPresenter { Name = "Content_1_Presenter" },
                        new ContentPresenter { Name = "Content_2_Presenter" }
                    }
                })
            };

            var ex = Record.Exception(() => target.ApplyTemplate());

            Assert.Null(ex);
        }

        [Fact]
        public void Replacing_Template_Releases_Events()
        {
            var p1 = new ContentPresenter { Name = "Content_1_Presenter" };
            var p2 = new ContentPresenter { Name = "Content_2_Presenter" };

            var callIndex = -1;
            var called = new bool[4];

            void Callback()
            {
                if (callIndex >= 0)
                    called[callIndex] = true;
            }

            var listMock = new Mock<IAvaloniaList<ILogical>>();
            listMock.Setup(l => l.Contains(It.IsAny<ILogical>())).Returns(false).Callback(Callback);
            var list = listMock.Object;

            var target = new TestControl(list)
            {
                Template = new FuncControlTemplate(_ => new Panel
                {
                    Children =
                    {
                        p1,
                        p2
                    }
                })
            };
            target.ApplyTemplate();

            callIndex = 0;
            p1.Content = new Control();
            p1.UpdateChild();

            callIndex = 1;
            p2.Content = new Control();
            p2.UpdateChild();

            target.Template = null;

            callIndex = 2;
            p1.Content = new Control();
            p1.UpdateChild();

            callIndex = 3;
            p2.Content = new Control();
            p2.UpdateChild();


            Assert.Equal(new[] { true, true, false, false }, called);
        }

        private class TestControl : TemplatedControl
        {
            public static readonly StyledProperty<object> Content1Property =
                AvaloniaProperty.Register<TestControl, object>(nameof(Content1));

            public static readonly StyledProperty<object> Content2Property =
                AvaloniaProperty.Register<TestControl, object>(nameof(Content2));


            static TestControl()
            {
                ContentControlMixin.Attach<TestControl>(Content1Property, x => x.GetLogicalChildren(), "Content_1_Presenter");
                ContentControlMixin.Attach<TestControl>(Content2Property, x => x.GetLogicalChildren(), "Content_2_Presenter");
            }

            private IAvaloniaList<ILogical> _mock;

            public TestControl()
            {
            }

            public TestControl(IAvaloniaList<ILogical> mock)
            {
                _mock = mock;
            }

            public IAvaloniaList<ILogical> GetLogicalChildren()
            {
                return _mock ?? LogicalChildren;
            }

            public object Content1
            {
                get { return GetValue(Content1Property); }
                set { SetValue(Content1Property, value); }
            }

            public object Content2
            {
                get { return GetValue(Content2Property); }
                set { SetValue(Content2Property, value); }
            }
        }
    }
}
