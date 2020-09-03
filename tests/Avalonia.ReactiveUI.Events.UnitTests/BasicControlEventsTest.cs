using System;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.ReactiveUI.Events.UnitTests
{
    public class BasicControlEventsTest
    {
        public class EventsControl : UserControl
        {
            public bool IsAttached { get; private set; }

            public EventsControl()
            {
                var attached = this
                    .Events()
                    .AttachedToVisualTree
                    .Select(args => true);

                this.Events()
                    .DetachedFromVisualTree
                    .Select(args => false)
                    .Merge(attached)
                    .Subscribe(marker => IsAttached = marker);
            }
        }

        [Fact]
        public void Should_Generate_Events_Wrappers()
        {
            var root = new TestRoot();
            var control = new EventsControl();
            Assert.False(control.IsAttached);

            root.Child = control;
            Assert.True(control.IsAttached);

            root.Child = null;
            Assert.False(control.IsAttached);
        }
    }
}
