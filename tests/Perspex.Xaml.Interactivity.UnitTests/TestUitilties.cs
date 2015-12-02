using System;
using Perspex;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Interactivity
{
    public static class TestUtilities
    {
        /// <summary>
        /// Handles the difference between InvalidOperationException in managed and native.
        /// </summary>
        public static void AssertThrowsInvalidOperationException(Action action)
        {
            Assert.Throws<InvalidOperationException>(action);
        }

        public static void AssertThrowsArgumentException(Action action)
        {
            Assert.Throws<ArgumentException>(action);
        }

        public static void AssertDetached(StubBehavior behavior)
        {
            Assert.Equal(1, behavior.DetachCount); // "The Behavior should be detached."
            Assert.Null(behavior.AssociatedObject); // "A Detached Behavior should have a null AssociatedObject."
        }

        public static void AssertNotDetached(StubBehavior behavior)
        {
            Assert.Equal(0, behavior.DetachCount); // "The Behavior should not be detached."
        }

        public static void AssertAttached(StubBehavior behavior, PerspexObject associatedObject)
        {
            Assert.Equal(1, behavior.AttachCount); // "The behavior should be attached."
            Assert.Equal(associatedObject, behavior.AssociatedObject); // "The AssociatedObject of the Behavior should be what it was attached to."
        }

        public static void AssertNotAttached(StubBehavior behavior)
        {
            Assert.Equal(0, behavior.AttachCount); // "The behavior should not be attached."
            Assert.Null(behavior.AssociatedObject); // "The AssociatedObject should be null for a non-attached Behavior."
        }
    }
}
