// -----------------------------------------------------------------------
// <copyright file="InteractiveTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Interactive.UnitTests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Collections;
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
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Id);
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
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Id);
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
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Id);
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
            EventHandler<RoutedEventArgs> handler = (s, e) => invoked.Add(((TestInteractive)s).Id);
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

        private TestInteractive CreateTree(
            RoutedEvent ev, 
            EventHandler<RoutedEventArgs> handler,
            RoutingStrategies handlerRoutes)
        {
            TestInteractive target;

            var tree = new TestInteractive
            {
                Id = "1",
                Children = new[]
                {
                    new TestInteractive
                    {
                        Id = "2a",
                    },
                    (target = new TestInteractive
                    {
                        Id = "2b",
                        Children = new[]
                        {
                            new TestInteractive
                            {
                                Id = "3",
                            },
                        },
                    }),
                }
            };

            foreach (var i in tree.GetSelfAndVisualDescendents().Cast<Interactive>())
            {
                i.AddHandler(ev, handler, handlerRoutes, false);
            }
            
            return target;
        }

        private class TestInteractive : Interactive
        {
            public string Id { get; set; }

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
        }
    }
}
