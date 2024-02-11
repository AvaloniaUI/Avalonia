using System.Collections.Generic;
using System.Reactive.Subjects;
using Avalonia.Data;
using Avalonia.PropertyStore;
using Avalonia.Styling;
using Microsoft.Reactive.Testing;
using Xunit;
using static Microsoft.Reactive.Testing.ReactiveTest;

#nullable enable

namespace Avalonia.Base.UnitTests.PropertyStore
{
    public class ValueStoreTests_Frames
    {
        [Fact]
        public void Adding_Frame_Raises_PropertyChanged()
        {
            var target = new Class1();
            var subject = new BehaviorSubject<string>("bar");
            var result = new List<PropertyChange>();
            var style = new Style
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "foo"),
                    new Setter(Class1.BarProperty, subject.ToBinding()),
                }
            };

            target.PropertyChanged += (s, e) =>
            {
                result.Add(new(e.Property, e.OldValue, e.NewValue));
            };

            var frame = InstanceStyle(style, target);
            target.GetValueStore().AddFrame(frame);

            Assert.Equal(new PropertyChange[]
            {
                new(Class1.FooProperty, "foodefault", "foo"),
                new(Class1.BarProperty, "bardefault", "bar"),
            }, result);
        }

        [Fact]
        public void Removing_Frame_Raises_PropertyChanged()
        {
            var target = new Class1();
            var subject = new BehaviorSubject<string>("bar");
            var result = new List<PropertyChange>();
            var style = new Style
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "foo"),
                    new Setter(Class1.BarProperty, subject.ToBinding()),
                }
            };
            var frame = InstanceStyle(style, target);
            target.GetValueStore().AddFrame(frame);

            target.PropertyChanged += (s, e) =>
            {
                result.Add(new(e.Property, e.OldValue, e.NewValue));
            };

            target.GetValueStore().RemoveFrame(frame);

            Assert.Equal(new PropertyChange[]
            {
                new(Class1.BarProperty, "bar", "bardefault"),
                new(Class1.FooProperty, "foo", "foodefault"),
            }, result);
        }

        [Fact]
        public void Removing_Frame_Unsubscribes_Binding()
        {
            var target = new Class1();
            var scheduler = new TestScheduler();
            var obs = scheduler.CreateColdObservable(OnNext(0, "bar"));
            var style = new Style
            {
                Setters =
                {
                    new Setter(Class1.FooProperty, "foo"),
                    new Setter(Class1.BarProperty, obs.ToBinding()),
                }
            };
            var frame = InstanceStyle(style, target);

            target.GetValueStore().AddFrame(frame);
            target.GetValueStore().RemoveFrame(frame);

            Assert.Single(obs.Subscriptions);
            Assert.Equal(0, obs.Subscriptions[0].Subscribe);
            Assert.NotEqual(Subscription.Infinite, obs.Subscriptions[0].Unsubscribe);
        }

        [Fact]
        public void Disposing_Binding_Removes_ImmediateValueFrame()
        {
            var target = new Class1();
            var source = new Binding { Priority = BindingPriority.Style };
            var expression = target.Bind(Class1.FooProperty, source);

            var valueStore = target.GetValueStore();
            Assert.Equal(1, valueStore.Frames.Count);
            Assert.IsType<ImmediateValueFrame>(valueStore.Frames[0]);

            expression.Dispose();

            Assert.Equal(0, valueStore.Frames.Count);
        }

        [Fact]
        public void Completing_Observable_Removes_ImmediateValueFrame()
        {
            var target = new Class1();
            var source = new BehaviorSubject<BindingValue<string>>("foo");

            target.Bind(Class1.FooProperty, source, BindingPriority.Animation);

            var valueStore = target.GetValueStore();
            Assert.Equal(1, valueStore.Frames.Count);
            Assert.IsType<ImmediateValueFrame>(valueStore.Frames[0]);

            source.OnCompleted();

            Assert.Equal(0, valueStore.Frames.Count);
        }

        private static StyleInstance InstanceStyle(Style style, StyledElement target)
        {
            var result = new StyleInstance(style, null, FrameType.Style);

            foreach (var setter in style.Setters)
                result.Add(setter.Instance(result, target));

            return result;
        }

        private class Class1 : StyledElement
        {
            public static readonly StyledProperty<string> FooProperty =
                AvaloniaProperty.Register<Class1, string>("Foo", "foodefault");

            public static readonly StyledProperty<string> BarProperty =
                AvaloniaProperty.Register<Class1, string>("Bar", "bardefault", true);
        }

        private record PropertyChange(
            AvaloniaProperty Property,
            object? OldValue,
            object? NewValue);
    }
}
