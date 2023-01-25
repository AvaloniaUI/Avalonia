using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Styling;
using Avalonia.Styling.Activators;
using Xunit;

namespace Avalonia.Base.UnitTests.Styling
{
    public class SelectorTests_Nesting
    {
        [Fact]
        public void Nesting_Class_Doesnt_Match_Parent_OfType_Selector()
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
        public void Or_Nesting_Class_Doesnt_Match_Parent_OfType_Selector()
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
        public void Or_Nesting_Child_OfType_Doesnt_Match_Parent_OfType_Selector()
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
        public void Double_Nesting_Class_Doesnt_Match_Grandparent_OfType_Selector()
        {
            var control = new Control2
            {
                Classes = { "foo", "bar" },
            };

            Style parent;
            Style nested;
            var grandparent = new Style(x => x.OfType<Control1>())
            {
                Children =
                {
                    (parent = new Style(x => x.Nesting().Class("foo"))
                    {
                        Children =
                        {
                            (nested = new Style(x => x.Nesting().Class("bar")))
                        }
                    })
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.NeverThisType, match.Result);
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
        public void Double_Nesting_Class_Matches()
        {
            var control = new Control1
            {
                Classes = { "foo", "bar" },
            };

            Style parent;
            Style nested;
            var grandparent = new Style(x => x.OfType<Control1>())
            {
                Children =
                {
                    (parent = new Style(x => x.Nesting().Class("foo"))
                    {
                        Children =
                        {
                            (nested = new Style(x => x.Nesting().Class("bar")))
                        }
                    })
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);

            var sink = new ActivatorSink(match.Activator);

            Assert.True(sink.Active);
            control.Classes.Remove("foo");
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

        [Fact]
        public void Adding_Child_With_No_Nesting_Selector_Fails()
        {
            var parent = new Style(x => x.OfType<Control1>());
            var child = new Style(x => x.Class("foo"));

            Assert.Throws<InvalidOperationException>(() => parent.Children.Add(child));
        }

        [Fact]
        public void Adding_Combinator_Selector_Child_With_No_Nesting_Selector_Fails()
        {
            var parent = new Style(x => x.OfType<Control1>());
            var child = new Style(x => x.Class("foo").Descendant().Class("bar"));

            Assert.Throws<InvalidOperationException>(() => parent.Children.Add(child));
        }

        [Fact]
        public void Adding_Or_Selector_Child_With_No_Nesting_Selector_Fails()
        {
            var parent = new Style(x => x.OfType<Control1>());
            var child = new Style(x => Selectors.Or(
                x.Nesting().Class("foo"),
                x.Class("bar")));

            Assert.Throws<InvalidOperationException>(() => parent.Children.Add(child));
        }

        [Fact]
        public void Can_Add_Child_Without_Nesting_Selector_To_Style_Without_Selector()
        {
            var parent = new Style();
            var child = new Style(x => x.Class("foo"));

            parent.Children.Add(child);
        }


        [Fact]
        public void Nesting_Not_Class_Matches()
        {
            var control = new Control1 { Classes = { "foo" } };
            Style nested;
            var parent = new Style(x => x.OfType<Control1>())
            {
                Children =
                {
                    (nested = new Style(x => x.Nesting().Not(y => y.Class("foo")))),
                }
            };

            var match = nested.Selector.Match(control, parent);
            Assert.Equal(SelectorMatchResult.Sometimes, match.Result);

            var sink = new ActivatorSink(match.Activator);

            Assert.False(sink.Active);
            control.Classes.Clear();
            Assert.True(sink.Active);
        }

        public class Control1 : Control
        {
        }

        public class Control2 : Control
        {
        }

        private class ActivatorSink : IStyleActivatorSink
        {
            public ActivatorSink(IStyleActivator source)
            {
                source.Subscribe(this);
                Active = source.GetIsActive();
            }


            public bool Active { get; private set; }
            public void OnNext(bool value) => Active = value;
        }
    }
}
