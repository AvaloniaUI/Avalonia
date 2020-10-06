﻿using System.Linq;
using Avalonia.Collections;
using Xunit;

namespace Avalonia.Base.UnitTests.Collections
{
    public class AvaloniaListExtenionsTests
    {
#pragma warning disable CS0618 // Type or member is obsolete
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

        [Theory]
        [InlineData(0, 2, 3)]
        [InlineData(0, 2, 4)]
        [InlineData(0, 2, 5)]
        [InlineData(0, 4, 4)]
        [InlineData(1, 2, 0)]
        [InlineData(1, 2, 4)]
        [InlineData(1, 2, 5)]
        [InlineData(1, 4, 0)]
        [InlineData(2, 2, 0)]
        [InlineData(2, 2, 1)]
        [InlineData(2, 2, 3)]
        [InlineData(2, 2, 4)]
        [InlineData(2, 2, 5)]
        [InlineData(4, 2, 0)]
        [InlineData(4, 2, 1)]
        [InlineData(4, 2, 3)]
        [InlineData(5, 1, 0)]
        [InlineData(5, 1, 3)]
        public void CreateDerivedList_Handles_MoveRange(int oldIndex, int count, int newIndex)
        {
            var source = new AvaloniaList<int>(new[] { 0, 1, 2, 3, 4, 5 });
            var target = source.CreateDerivedList(x => new Wrapper(x));

            source.MoveRange(oldIndex, count, newIndex);

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
#pragma warning restore CS0618 // Type or member is obsolete


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
