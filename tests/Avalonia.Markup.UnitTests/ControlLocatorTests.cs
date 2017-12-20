// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Markup.UnitTests
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
            
            var locator = ControlLocator.Track(relativeTo, "target");
            var result = await locator.Take(1);

            Assert.Same(target, result);
            Assert.Equal(0, root.NameScopeRegisteredSubscribers);
            Assert.Equal(0, root.NameScopeUnregisteredSubscribers);
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

            var locator = ControlLocator.Track(relativeTo, "target");
            var target = new TextBlock { Name = "target" };
            var result = new List<IControl>();

            using (locator.Subscribe(x => result.Add(x)))
            {
                panel.Children.Add(target);
            }

            Assert.Equal(new[] { null, target }, result);
            Assert.Equal(0, root.NameScopeRegisteredSubscribers);
            Assert.Equal(0, root.NameScopeUnregisteredSubscribers);
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

            var locator = ControlLocator.Track(relativeTo, "target");
            var result = new List<IControl>();
            locator.Subscribe(x => result.Add(x));

            var other = new TextBlock { Name = "target" };
            panel.Children.Remove(target);
            panel.Children.Add(other);

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

            var locator = ControlLocator.Track(relativeTo, "target");
            var target = new TextBlock { Name = "target" };
            var result = new List<IControl>();

            using (locator.Subscribe(x => result.Add(x)))
            {
                ((StackPanel)root1.Child).Children.Remove(relativeTo);
                ((StackPanel)root2.Child).Children.Add(relativeTo);
            }

            Assert.Equal(new[] { target1, null, target2 }, result);
            Assert.Equal(0, root1.NameScopeRegisteredSubscribers);
            Assert.Equal(0, root1.NameScopeUnregisteredSubscribers);
        }
    }
}
