// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Data;
using Xunit;

namespace Avalonia.Base.UnitTests
{
    public class AvaloniaObjectTests_Listen
    {
        [Fact]
        public void Listen_Does_Not_Fire_Immediately()
        {
            var target = new Class1();
            var raised = 0;

            target.Listen(Class1.FooProperty).Subscribe(_ => ++raised);

            Assert.Equal(0, raised);
        }

        [Fact]
        public void Listener_Fires_On_Property_Change()
        {
            var target = new Class1();
            var raised = 0;

            target.Listen(Class1.FooProperty).Subscribe(x =>
            {
                Assert.Equal("newvalue", x.NewValue.Value);
                Assert.Equal("foodefault", x.OldValue.Value);
                Assert.Equal(BindingPriority.LocalValue, x.Priority);
                Assert.True(x.IsActiveValueChange);
                Assert.False(x.IsOutdated);
                ++raised;
            });

            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Listener_Fires_On_Non_Active_Property_Value_Change()
        {
            var target = new Class1();
            var raised = 0;

            target.Listen(Class1.FooProperty).Skip(1).Subscribe(x =>
            {
                Assert.Equal("styled", x.NewValue.Value);
                Assert.False(x.OldValue.HasValue);
                Assert.Equal(BindingPriority.Style, x.Priority);
                Assert.False(x.IsActiveValueChange);
                Assert.False(x.IsOutdated);
                ++raised;
            });

            target.SetValue(Class1.FooProperty, "newvalue");
            target.SetValue(Class1.FooProperty, "styled", BindingPriority.Style);

            Assert.Equal(1, raised);
        }

        [Fact]
        public void Listener_Fires_On_All_Binding_Property_Changes()
        {
            var target = new Class1();
            var changes = new List<AvaloniaPropertyChangedEventArgs<string>>();
            var style = new Subject<BindingValue<string>>();
            var animation = new Subject<BindingValue<string>>();
            var templatedParent = new Subject<BindingValue<string>>();

            target.Bind(Class1.FooProperty, style, BindingPriority.Style);
            target.Bind(Class1.FooProperty, animation, BindingPriority.Animation);
            target.Bind(Class1.FooProperty, templatedParent, BindingPriority.TemplatedParent);

            target.Listen(Class1.FooProperty).Subscribe(x => changes.Add(x));

            style.OnNext("style1");
            templatedParent.OnNext("tp1");
            animation.OnNext("a1");
            templatedParent.OnNext("tp2");
            templatedParent.OnCompleted();
            animation.OnNext("a2");
            style.OnNext("style2");
            style.OnCompleted();
            animation.OnCompleted();

            Assert.Equal(
                new[] { true, true, true, false, false, true, false, false, true },
                changes.Select(x => x.IsActiveValueChange).ToList());
            Assert.Equal(
                new[] { "style1", "tp1", "a1", "tp2", "$unset", "a2", "style2", "$unset", "foodefault" },
                changes.Select(x => x.NewValue.GetValueOrDefault("$unset")).ToList());
            Assert.Equal(
                new[] { "foodefault", "style1", "tp1", "$unset", "$unset", "a1", "$unset", "$unset", "a2" },
                changes.Select(x => x.OldValue.GetValueOrDefault("$unset")).ToList());
        }

        [Fact]
        public void Listener_Signals_Outdated_Change()
        {
            var target = new Class1();
            var raised = 0;

            target.Listen(Class1.FooProperty).Subscribe(x =>
            {
                if (x.NewValue.Value == "value1")
                {
                    // In the handler for the change to "value1", set the value to "value2".
                    target.SetValue(Class1.FooProperty, "value2");
                }
            });

            target.Listen(Class1.FooProperty).Subscribe(x =>
            {
                // This handler was added after the handler which changes the value.
                // It should receive both changes in order, with the first one marked
                // outdated because by the time this handler receives the notification,
                // the value has already been set to "value2".
                if (raised == 0)
                {
                    Assert.Equal("value1", x.NewValue.Value);
                    Assert.True(x.IsOutdated);
                }
                else if (raised == 1)
                {
                    Assert.Equal("value2", x.NewValue.Value);
                    Assert.False(x.IsOutdated);
                }

                ++raised;
            });


            target.SetValue(Class1.FooProperty, "value1");

            Assert.Equal(2, raised);
        }

        [Fact]
        public void Listener_Signals_Property_Change_Only_For_Correct_Property()
        {
            var target = new Class1();
            var result = new List<string>();

            target.Listen(Class1.FooProperty).Subscribe(x => result.Add(x.NewValue.Value));
            target.SetValue(Class1.BarProperty, "newvalue");

            Assert.Empty(result);
        }

        [Fact]
        public void Listener_Dispose_Stops_Property_Changes()
        {
            Class1 target = new Class1();
            bool raised = false;

            target.Listen(Class1.FooProperty)
                  .Subscribe(x => raised = true)
                  .Dispose();
            raised = false;
            target.SetValue(Class1.FooProperty, "newvalue");

            Assert.False(raised);
        }

        [Fact]
        public void All_Property_Listen_Doesnt_Signal_Anything_Before_Property_Changes()
        {
            var target = new Class1();
            var listener = new AllPropertyListener();

            target.Listen(listener);

            Assert.Empty(listener.Received);
        }

        [Fact]
        public void All_Property_Listen_Fires_On_Property_Change()
        {
            var target = new Class1();
            var listener = new AllPropertyListener();

            target.Listen(listener);
            target.SetValue(Class1.FooProperty, "newvalue");
            target.SetValue(Class1.BarProperty, "baaa", BindingPriority.Style);

            Assert.Equal(2, listener.Received.Count);

            var change = Assert.IsType<AvaloniaPropertyChangedEventArgs<string>>(listener.Received[0]);
            Assert.Equal("newvalue", change.NewValue.Value);
            Assert.Equal("foodefault", change.OldValue.Value);
            Assert.Equal(BindingPriority.LocalValue, change.Priority);
            Assert.True(change.IsActiveValueChange);
            Assert.False(change.IsOutdated);

            change = Assert.IsType<AvaloniaPropertyChangedEventArgs<string>>(listener.Received[1]);
            Assert.Equal("baaa", change.NewValue.Value);
            Assert.Equal("bardefault", change.OldValue.Value);
            Assert.Equal(BindingPriority.Style, change.Priority);
            Assert.True(change.IsActiveValueChange);
            Assert.False(change.IsOutdated);
        }

        private class Class1 : AvaloniaObject
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<Class1, string>("Bar", "bardefault");
        }

        private class AllPropertyListener : IAvaloniaPropertyListener
        {
            public List<object> Received { get; } = new List<object>();

            public void PropertyChanged<T>(AvaloniaPropertyChangedEventArgs<T> change)
            {
                Received.Add(change);
            }
        }
    }
}
