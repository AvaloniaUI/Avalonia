using Avalonia.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class SingleOrQueueTests
    {
        [Fact]
        public void New_SingleOrQueue_Is_Empty()
        {
            Assert.True(new SingleOrQueue<object>().Empty);
        }

        [Fact]
        public void Dequeue_Throws_When_Empty()
        {
            var queue = new SingleOrQueue<object>();

            Assert.Throws<InvalidOperationException>(() => queue.Dequeue());
        }

        [Fact]
        public void Enqueue_Adds_Element()
        {
            var queue = new SingleOrQueue<int>();

            queue.Enqueue(1);

            Assert.False(queue.Empty);

            Assert.Equal(1, queue.Dequeue());
        }

        [Fact]
        public void Multiple_Elements_Dequeued_In_Correct_Order()
        {
            var queue = new SingleOrQueue<int>();

            queue.Enqueue(1);
            queue.Enqueue(2);
            queue.Enqueue(3);
            Assert.Equal(1, queue.Dequeue());
            Assert.Equal(2, queue.Dequeue());
            Assert.Equal(3, queue.Dequeue());
        }
    }
}
