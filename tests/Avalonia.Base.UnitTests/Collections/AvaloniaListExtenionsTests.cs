using System;
using System.Linq;
using Avalonia.Collections;
using Xunit;

namespace Avalonia.Base.UnitTests.Collections
{
    public class AvaloniaListExtenionsTests
    {
        [Fact]
        public void CreateDerivedList_Creates_Initial_Items()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));
            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_Add()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.Add(4);

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_Insert()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.Insert(1, 4);

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_Remove()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.Remove(2);

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_RemoveRange()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.RemoveRange(1, 2);

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_Move()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.Move(2, 0);

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_MoveRange()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.MoveRange(1, 2, 0);

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_Replace()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source[1] = 4;

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        [Fact]
        public void CreateDerivedList_Handles_Clear()
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.Clear();

            var result = target.Select(x => x.Value).ToList();

            Assert.Equal(source, result);
        }

        private class Wrapper
        {
            public Wrapper(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }
    }
}
