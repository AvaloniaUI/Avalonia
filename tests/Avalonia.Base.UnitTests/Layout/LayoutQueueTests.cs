using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Base.UnitTests.Layout
{
    public class LayoutQueueTests
    {
        private class TestLayoutable : Control
        {
            public new string Name { get; }
            
            public TestLayoutable(string name)
            {
                Name = name;
            }
            
            public void SetMeasureValid(bool value)
            {
                // Use reflection or internal access to set IsMeasureValid for testing
                // For now, we'll rely on InvalidateMeasure behavior
            }
        }
        
        [Fact]
        public void Should_Enqueue()
        {
            var target = new LayoutQueue(isMeasureQueue: true);
            var refQueue = new Queue<Layoutable>();
            var items = new[] { new TestLayoutable("1"), new TestLayoutable("2"), new TestLayoutable("3") };

            foreach (var item in items)
            {
                target.Enqueue(item);
                refQueue.Enqueue(item);
            }

            Assert.Equal(refQueue, target);
        }

        [Fact]
        public void Should_Dequeue()
        {
            var target = new LayoutQueue(isMeasureQueue: true);
            var refQueue = new Queue<Layoutable>();
            var items = new[] { new TestLayoutable("1"), new TestLayoutable("2"), new TestLayoutable("3") };

            foreach (var item in items)
            {
                target.Enqueue(item);
                refQueue.Enqueue(item);
            }

            while (refQueue.Count > 0)
            {
                Assert.Equal(refQueue.Dequeue(), target.Dequeue());
            }
        }

        [Fact]
        public void Should_Enqueue_UniqueElements()
        {
            var target = new LayoutQueue(isMeasureQueue: true);

            var item1 = new TestLayoutable("1");
            var item2 = new TestLayoutable("2");
            var item3 = new TestLayoutable("3");
            var items = new[] { item1, item2, item3, item1 }; // item1 appears twice

            foreach (var item in items)
            {
                target.Enqueue(item);
            }

            Assert.Equal(3, target.Count);
            Assert.Equal(new Layoutable[] { item1, item2, item3 }, target);
        }

        [Fact]
        public void Shouldnt_Enqueue_More_Than_Limit_In_Loop()
        {
            var target = new LayoutQueue(isMeasureQueue: true);
            var item = new TestLayoutable("Foo");

            //1
            target.Enqueue(item);

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue(item);
            target.Dequeue();

            //3
            target.Enqueue(item);

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added
            target.Enqueue(item);

            Assert.Equal(0, target.Count);
        }

        [Fact]
        public void Shouldnt_Count_Unique_Enqueue_For_Limit_In_Loop()
        {
            var target = new LayoutQueue(isMeasureQueue: true);
            var item = new TestLayoutable("Foo");

            //1
            target.Enqueue(item);

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue(item);
            target.Enqueue(item); // duplicate, should be ignored
            target.Dequeue();

            //3
            target.Enqueue(item);
            target.Enqueue(item); // duplicate, should be ignored

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added
            target.Enqueue(item);

            Assert.Equal(0, target.Count);
        }

        [Fact]
        public void Should_Enqueue_When_Condition_True_After_Loop_When_Limit_Met()
        {
            var target = new LayoutQueue(isMeasureQueue: true);
            var item = new TestLayoutable("Foo");
            // Item starts with IsMeasureValid = false (default), so shouldEnqueue returns true

            //1
            target.Enqueue(item);

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue(item);
            target.Dequeue();

            //3
            target.Enqueue(item);

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added to queue
            target.Enqueue(item);

            Assert.Equal(0, target.Count);

            target.EndLoop();

            //after loop should be added once (because IsMeasureValid is false)
            Assert.Equal(1, target.Count);
            Assert.Equal(item, target.First());
        }

        [Fact]
        public void Shouldnt_Enqueue_When_Condition_False_After_Loop_When_Limit_Met()
        {
            var target = new LayoutQueue(isMeasureQueue: true);
            var item = new TestLayoutable("Foo");
            
            // We need to simulate a measured item - use a helper
            using var app = UnitTestApplication.Start(TestServices.MockPlatformRenderInterface);
            var root = new TestRoot { Child = item };
            root.LayoutManager.ExecuteInitialLayoutPass();
            // Now item.IsMeasureValid should be true
            
            // Reset queue state
            item.IsInMeasureQueue = false;
            item.MeasureQueueCount = 0;

            //1
            target.Enqueue(item);

            Assert.Equal(1, target.Count);

            target.BeginLoop(3);

            target.Dequeue();

            //2
            target.Enqueue(item);
            target.Dequeue();

            //3
            target.Enqueue(item);

            Assert.Equal(1, target.Count);

            target.Dequeue();

            //4 more than limit shouldn't be added
            target.Enqueue(item);

            Assert.Equal(0, target.Count);

            target.EndLoop();

            // Because IsMeasureValid is true, shouldn't be re-enqueued
            Assert.Equal(0, target.Count);
        }
    }
}