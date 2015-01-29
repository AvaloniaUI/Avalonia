// -----------------------------------------------------------------------
// <copyright file="SelectorTests.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using System.Linq;
    using System.Reactive.Linq;
    using Moq;
    using Perspex.Styling;
    using Xunit;

    public class SelectorTests_Class
    {
        [Fact]
        public void Class_Priority_Is_StyleTrigger()
        {
            var control = new Control1();
            var target = new Selector().Class("foo");

            Assert.Equal(BindingPriority.StyleTrigger, target.Priority);
        }

        [Fact]
        public void Class_Matches_Control_With_Class()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
            };

            var target = new Selector().Class("foo");

            Assert.True(ActivatorValue(target, control));
        }

        [Fact]
        public void Class_Doesnt_Match_Control_Without_Class()
        {
            var control = new Control1
            {
                Classes = new Classes { "bar" },
            };

            var target = new Selector().Class("foo");

            Assert.False(ActivatorValue(target, control));
        }

        [Fact]
        public void Class_Matches_Control_With_TemplatedParent()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
                TemplatedParent = new Mock<ITemplatedControl>().Object,
            };

            var target = new Selector().Class("foo");

            Assert.True(ActivatorValue(target, control));
        }

        [Fact]
        public void Class_Tracks_Additions()
        {
            var control = new Control1();

            var target = new Selector().Class("foo");
            var activator = target.GetActivator(control);

            Assert.False(ActivatorValue(target, control));
            control.Classes.Add("foo");
            Assert.True(ActivatorValue(target, control));
        }

        [Fact]
        public void Class_Tracks_Removals()
        {
            var control = new Control1
            {
                Classes = new Classes { "foo" },
            };

            var target = new Selector().Class("foo");
            var activator = target.GetActivator(control);

            Assert.True(ActivatorValue(target, control));
            control.Classes.Remove("foo");
            Assert.False(ActivatorValue(target, control));
        }

        [Fact]
        public void Multiple_Classes()
        {
            var control = new Control1();
            var target = new Selector().Class("foo").Class("bar");
            var activator = target.GetActivator(control);

            Assert.False(ActivatorValue(target, control));
            control.Classes.Add("foo");
            Assert.False(ActivatorValue(target, control));
            control.Classes.Add("bar");
            Assert.True(ActivatorValue(target, control));
            control.Classes.Remove("bar");
            Assert.False(ActivatorValue(target, control));
        }

        private static bool ActivatorValue(Selector selector, IStyleable control)
        {
            return selector.GetActivator(control).Take(1).ToEnumerable().Single();
        }

        public class Control1 : TestControlBase
        {
        }
    }
}
