﻿// -----------------------------------------------------------------------
// <copyright file="InteractiveTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Interactivity.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Interactivity;
    using Perspex.VisualTree;
    using Xunit;

    public class InteractiveTests
    {
        [Fact]
        public void Direct_Event_Should_Go_Straight_To_Source()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Direct, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = this.CreateTree(ev, handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b" }, invoked);
        }

        [Fact]
        public void Direct_Event_Should_Have_Route_Set_To_Direct()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Direct, typeof(RoutedEventArgs), typeof(TestInteractive));
            bool called = false;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                Assert.Equal(RoutingStrategies.Direct, e.Route);
                called = true;
            };

            var target = this.CreateTree(ev, handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.True(called);
        }

        [Fact]
        public void Bubbling_Event_Should_Bubble_Up()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Bubble, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = this.CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b", "1" }, invoked);
        }

        [Fact]
        public void Tunneling_Event_Should_Tunnel()
        {
            var ev = new RoutedEvent("test", RoutingStrategies.Tunnel, typeof(RoutedEventArgs), typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = this.CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b" }, invoked);
        }

        [Fact]
        public void Tunneling_Bubbling_Event_Should_Tunnel_Then_Bubble_Up()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);
            var target = this.CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b", "2b", "1" }, invoked);
        }

        [Fact]
        public void Events_Should_Have_Route_Set()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<RoutingStrategies>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(e.Route);
            var target = this.CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[]
            {
                RoutingStrategies.Tunnel,
                RoutingStrategies.Tunnel,
                RoutingStrategies.Bubble,
                RoutingStrategies.Bubble,
            },
            invoked);
        }

        [Fact]
        public void Direct_Subscription_Should_Not_Catch_Tunneling_Or_Bubbling()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var count = 0;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                ++count;
            };

            var target = this.CreateTree(ev, handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(0, count);
        }

        [Fact]
        public void Bubbling_Subscription_Should_Not_Catch_Tunneling()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var count = 0;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                Assert.Equal(RoutingStrategies.Bubble, e.Route);
                ++count;
            };

            var target = this.CreateTree(ev, handler, RoutingStrategies.Bubble);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(2, count);
        }

        [Fact]
        public void Tunneling_Subscription_Should_Not_Catch_Bubbling()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var count = 0;

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                Assert.Equal(RoutingStrategies.Tunnel, e.Route);
                ++count;
            };

            var target = this.CreateTree(ev, handler, RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(2, count);
        }

        [Fact]
        public void Event_Should_Should_Keep_Propogating_To_HandedEventsToo_Handlers()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();

            EventHandler<RoutedEventArgs> handler = (s, e) =>
            {
                invoked.Add(((TestInteractive)s).Name);
                e.Handled = true;
            };

            var target = this.CreateTree(ev, handler, RoutingStrategies.Bubble | RoutingStrategies.Tunnel, true);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b", "2b", "1" }, invoked);
        }

        [Fact]
        public void Direct_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Direct,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);

            var target = this.CreateTree(ev, null, 0);

            ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Direct);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b" }, invoked);
        }

        [Fact]
        public void Tunneling_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);

            var target = this.CreateTree(ev, null, 0);

            ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Tunnel);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "1", "2b" }, invoked);
        }

        [Fact]
        public void Bubbling_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(RoutedEventArgs),
                typeof(TestInteractive));
            var invoked = new List<string>();
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Name);

            var target = this.CreateTree(ev, null, 0);

            ev.AddClassHandler(typeof(TestInteractive), handler, RoutingStrategies.Bubble);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.Equal(new[] { "2b", "1" }, invoked);
        }

        [Fact]
        public void Typed_Class_Handlers_Should_Be_Called()
        {
            var ev = new RoutedEvent<RoutedEventArgs>(
                "test",
                RoutingStrategies.Bubble | RoutingStrategies.Tunnel,
                typeof(TestInteractive));

            var target = this.CreateTree(ev, null, 0);

            ev.AddClassHandler<TestInteractive>(x => x.ClassHandler, RoutingStrategies.Bubble);

            var args = new RoutedEventArgs(ev, target);
            target.RaiseEvent(args);

            Assert.True(target.ClassHandlerInvoked);
            Assert.True(target.GetVisualParent<TestInteractive>().ClassHandlerInvoked);
        }

        private TestInteractive CreateTree(
            RoutedEvent ev,
            EventHandler<RoutedEventArgs> handler,
            RoutingStrategies handlerRoutes,
            bool handledEventsToo = false)
        {
            TestInteractive target;

            var tree = new TestInteractive
            {
                Name = "1",
                Children = new[]
                {
                    new TestInteractive
                    {
                        Name = "2a",
                    },
                    (target = new TestInteractive
                    {
                        Name = "2b",
                        Children = new[]
                        {
                            new TestInteractive
                            {
                                Name = "3",
                            },
                        },
                    }),
                }
            };

            if (handler != null)
            {
                foreach (var i in tree.GetSelfAndVisualDescendents().Cast<Interactive>())
                {
                    i.AddHandler(ev, handler, handlerRoutes, handledEventsToo);
                }
            }

            return target;
        }

        private class TestInteractive : Interactive
        {
            public string Name { get; set; }

            public bool ClassHandlerInvoked { get; private set; }

            public IEnumerable<IVisual> Children
            {
                get
                {
                    return ((IVisual)this).VisualChildren.AsEnumerable();
                }

                set
                {
                    this.AddVisualChildren(value.Cast<Visual>());
                }
            }

            public void ClassHandler(RoutedEventArgs e)
            {
                this.ClassHandlerInvoked = true;
            }
        }
    }
}
