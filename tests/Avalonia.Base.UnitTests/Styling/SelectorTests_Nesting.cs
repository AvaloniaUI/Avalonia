using System;
using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Styling.Activators;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Nesting
    {
        [Fact]
        public void Nesting_Class_Doesnt_Match_Parent_Selector()
        {
            var control = new Control2();
            Style nested;
            var parent = new Style(x => x.OfType<Control1>())
            {
                Children =
                {
                    (nested = new Style(x => x.Nesting().Class("foo"))),
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.NeverThisType, match.Result);
        }

        [Fact]
        public void Or_Nesting_Class_Doesnt_Match_Parent_Selector()
        {
            var control = new Control2();
            Style nested;
            var parent = new Style(x => x.OfType<Control1>())
            {
                Children =
                {
                    (nested = new Style(x => Selectors.Or(
                        x.Nesting().Class("foo"),
                        x.Nesting().Class("bar")))),
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.NeverThisType, match.Result);
        }

        [Fact]
        public void Or_Nesting_Child_OfType_Does_Not_Match_Parent_Selector()
        {
            var control = new Control1();
            var panel = new DockPanel { Children = { control } };
            Style nested;
            var parent = new Style(x => x.OfType<Panel>())
            {
                Children =
                {
                    (nested = new Style(x => Selectors.Or(
                        x.Nesting().Child().OfType<Control1>(),
                        x.Nesting().Child().OfType<Control1>()))),
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.NeverThisInstance, match.Result);
        }

        [Fact]
        public void Nesting_Class_Matches()
        {
            var control = new Control1 { Classes = { "foo" } };
            Style nested;
            var parent = new Style(x => x.OfType<Control1>())
            {
                Children =
                {
                    (nested = new Style(x => x.Nesting().Class("foo"))),
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);

            var sink = new ActivatorSink(match.Activator);

            Assert.True(sink.Active);
            control.Classes.Clear();
            Assert.False(sink.Active);
        }

        [Fact]
        public void Or_Nesting_Class_Matches()
        {
            var control = new Control1 { Classes = { "foo" } };
            Style nested;
            var parent = new Style(x => x.OfType<Control1>())
            {
                Children =
                {
                    (nested = new Style(x => Selectors.Or(
                        x.Nesting().Class("foo"),
                        x.Nesting().Class("bar")))),
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);

            var sink = new ActivatorSink(match.Activator);

            Assert.True(sink.Active);
            control.Classes.Clear();
            Assert.False(sink.Active);
        }

        [Fact]
        public void Or_Nesting_Child_OfType_Matches()
        {
            var control = new Control1 { Classes = { "foo" } };
            var panel = new Panel { Children = { control } };
            Style nested;
            var parent = new Style(x => x.OfType<Panel>())
            {
                Children =
                {
                    (nested = new Style(x => Selectors.Or(
                        x.Nesting().Child().OfType<Control1>(),
                        x.Nesting().Child().OfType<Control1>()))),
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.AlwaysThisInstance, match.Result);
        }

        [Fact]
        public void Nesting_With_No_Parent_Style_Fails()
        {
            var control = new Control1();
            var style = new Style(x => x.Nesting().OfType<Control1>());

            Assert.Throws<InvalidOperationException>(() => style.Selector.Match(control, null));
        }

        [Fact]
        public void Nesting_With_No_Parent_Selector_Fails()
        {
            var control = new Control1();
            Style nested;
            var parent = new Style
            {
                Children =
                {
                    (nested = new Style(x => x.Nesting().Class("foo"))),
                }
            };

            Assert.Throws<InvalidOperationException>(() => nested.Selector.Match(control, parent));
        }

        public class Control1 : Control
        {
        }

        public class Control2 : Control
        {
        }

        private class ActivatorSink : IStyleActivatorSink
        {
            public ActivatorSink(IStyleActivator source) => source.Subscribe(this);
            public bool Active { get; private set; }
            public void OnNext(bool value, int tag) => Active = value;
        }
    }
}
