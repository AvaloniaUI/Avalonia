// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Styling.UnitTests
{
    public class ControlLocatorTests
    {
        [Fact]
        public async Task Track_By_Name_Should_Find_Control_Added_Earlier()
        {
            TextBlock target;
            TextBlock relativeTo;

            var root = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (target = new TextBlock { Name = "target" }),
                        (relativeTo = new TextBlock { Name = "start" }),
                    }
                }
            };
            var scope = Register(root, relativeTo);
            Register(root, target);
            
            var locator = ControlLocator.Track(relativeTo, "target");
            var result = await locator.Take(1);

            Assert.Same(target, result);
            Assert.Equal(0, scope.RegisteredSubscribers);
            Assert.Equal(0, scope.UnregisteredSubscribers);
        }


        
        [Fact]
        public void Track_By_Name_Should_Find_Control_Added_Later()
        {
            StackPanel panel;
            TextBlock relativeTo;

            var root = new TestRoot
            {
                Child = (panel = new StackPanel
                {
                    Children =
                    {
                        (relativeTo = new TextBlock
                        {
                            Name = "start"
                        }),
                    }
                })
            };
            var scope = Register(root, relativeTo);

            var locator = ControlLocator.Track(relativeTo, "target");
            var target = new TextBlock { Name = "target" };
            var result = new List<ILogical>();

            using (locator.Subscribe(x => result.Add(x)))
            {
                panel.Children.Add(target);
                Register(root, target);
            }

            Assert.Equal(new[] { null, target }, result);
            Assert.Equal(0, scope.RegisteredSubscribers);
            Assert.Equal(0, scope.UnregisteredSubscribers);
        }

        [Fact]
        public void Track_By_Name_Should_Track_Removal_And_Readd()
        {
            StackPanel panel;
            TextBlock target;
            TextBlock relativeTo;

            var root = new TestRoot
            {
                Child = panel = new StackPanel
                {
                    Children =
                    {
                        (target = new TextBlock { Name = "target" }),
                        (relativeTo = new TextBlock { Name = "start" }),
                    }
                }
            };
            var scope = Register(root, target);
            Register(root, relativeTo);
            
            var locator = ControlLocator.Track(relativeTo, "target");
            var result = new List<ILogical>();
            locator.Subscribe(x => result.Add(x));

            var other = new TextBlock { Name = "target" };
            panel.Children.Remove(target);
            scope.Unregister(target.Name);
            panel.Children.Add(other);
            Register(root, other);

            Assert.Equal(new[] { target, null, other }, result);
        }

        [Fact]
        public void Track_By_Name_Should_Find_Control_When_Tree_Changed()
        {
            TextBlock target1;
            TextBlock target2;
            TextBlock relativeTo;

            var root1 = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (relativeTo = new TextBlock
                        {
                            Name = "start"
                        }),
                        (target1 = new TextBlock { Name = "target" }),
                    }
                }
            };
            var scope1 = Register(root1, relativeTo);
            Register(root1, relativeTo);
            Register(root1, target1);

            var root2 = new TestRoot
            {
                Child = new StackPanel
                {
                    Children =
                    {
                        (target2 = new TextBlock { Name = "target" }),
                    }
                }
            };
            var scope2 = Register(root2, target2);

            var locator = ControlLocator.Track(relativeTo, "target");
            var result = new List<ILogical>();

            using (locator.Subscribe(x => result.Add(x)))
            {
                ((StackPanel)root1.Child).Children.Remove(relativeTo);
                scope1.Unregister(relativeTo.Name);
                ((StackPanel)root2.Child).Children.Add(relativeTo);
                Register(root2, relativeTo);
            }

            Assert.Equal(new[] { target1, null, target2 }, result);
            Assert.Equal(0, scope1.RegisteredSubscribers);
            Assert.Equal(0, scope1.UnregisteredSubscribers);
            Assert.Equal(0, scope2.RegisteredSubscribers);
            Assert.Equal(0, scope2.UnregisteredSubscribers);
        }

        TrackingNameScope Register(StyledElement anchor, StyledElement element)
        {
            var scope = (TrackingNameScope)NameScope.GetNameScope(anchor);
            if (scope == null)
                NameScope.SetNameScope(anchor, scope = new TrackingNameScope());
            NameScope.Register(anchor, element.Name, element);
            return scope;
        }
        
        class TrackingNameScope : INameScope
        {
            public int RegisteredSubscribers { get; private set; }
            public int UnregisteredSubscribers { get; private set; }
            private NameScope _inner = new NameScope();
            public event EventHandler<NameScopeEventArgs> Registered
            {
                add
                {
                    _inner.Registered += value;
                    RegisteredSubscribers++;
                }
                remove
                {
                    _inner.Registered -= value;
                    RegisteredSubscribers--;
                }
            }

            public event EventHandler<NameScopeEventArgs> Unregistered
            {
                add
                {
                    _inner.Unregistered += value;
                    UnregisteredSubscribers++;
                }
                remove
                {
                    _inner.Unregistered -= value;
                    UnregisteredSubscribers--;
                }
            }

            public void Register(string name, object element) => _inner.Register(name, element);

            public object Find(string name) => _inner.Find(name);

            public void Unregister(string name) => _inner.Unregister(name);
        }
    }
}
