// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
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
                Template = new FuncControlTemplate((_, scope) => new Panel
                {
                    Children =
                    {
                        new ContentPresenter {Name = "Content_1_Presenter"}.RegisterInNameScope(scope),
                        new ContentPresenter {Name = "Content_2_Presenter"}.RegisterInNameScope(scope)
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
            var target = new TestControl
            {
                Template = new FuncControlTemplate((_, scope) => new Panel
                {
                    Children =
                    {
                        p1.RegisterInNameScope(scope),
                        p2.RegisterInNameScope(scope)
                    }
                })
            };
            target.ApplyTemplate();

            Control tc;

            p1.Content = tc = new Control();
            p1.UpdateChild();
            Assert.Contains(tc, target.GetLogicalChildren());

            p2.Content = tc = new Control();
            p2.UpdateChild();
            Assert.Contains(tc, target.GetLogicalChildren());

            target.Template = null;

            p1.Content = tc = new Control();
            p1.UpdateChild();
            Assert.DoesNotContain(tc, target.GetLogicalChildren());

            p2.Content = tc = new Control();
            p2.UpdateChild();
            Assert.DoesNotContain(tc, target.GetLogicalChildren());

        }

        private class TestControl : TemplatedControl
        {
            public static readonly StyledProperty<object> Content1Property =
                AvaloniaProperty.Register<TestControl, object>(nameof(Content1));

            public static readonly StyledProperty<object> Content2Property =
                AvaloniaProperty.Register<TestControl, object>(nameof(Content2));

            static TestControl()
            {
                ContentControlMixin.Attach<TestControl>(Content1Property, x => x.LogicalChildren, "Content_1_Presenter");
                ContentControlMixin.Attach<TestControl>(Content2Property, x => x.LogicalChildren, "Content_2_Presenter");
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
