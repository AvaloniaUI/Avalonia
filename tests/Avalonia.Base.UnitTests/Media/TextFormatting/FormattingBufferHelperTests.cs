using System;
using System.Collections.Generic;
using System.Reflection;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.TextFormatting
{
    public class FormattingBufferHelperTests
    {
        public static TheoryData<int> SmallSizes => new() { 1, 500, 10_000, 125_000 };
        public static TheoryData<int> LargeSizes => new() { 500_000, 1_000_000 };

        [Theory]
        [MemberData(nameof(SmallSizes))]
        public void Should_Keep_Small_Buffer_List(int itemCount)
        {
            var capacity = FillAndClearList(itemCount);

            Assert.True(capacity >= itemCount);
        }

        [Theory]
        [MemberData(nameof(LargeSizes))]
        public void Should_Reset_Large_Buffer_List(int itemCount)
        {
            var capacity = FillAndClearList(itemCount);

            Assert.Equal(0, capacity);
        }

        private static int FillAndClearList(int itemCount)
        {
            var list = new List<int>();

            for (var i = 0; i < itemCount; ++i)
            {
                list.Add(i);
            }

            FormattingBufferHelper.ClearThenResetIfTooLarge(list);

            return list.Capacity;
        }

        [Theory]
        [MemberData(nameof(SmallSizes))]
        public void Should_Keep_Small_Buffer_ArrayBuilder(int itemCount)
        {
            var capacity = FillAndClearArrayBuilder(itemCount);

            Assert.True(capacity >= itemCount);
        }

        [Theory]
        [MemberData(nameof(LargeSizes))]
        public void Should_Reset_Large_Buffer_ArrayBuilder(int itemCount)
        {
            var capacity = FillAndClearArrayBuilder(itemCount);

            Assert.Equal(0, capacity);
        }

        private static int FillAndClearArrayBuilder(int itemCount)
        {
            var arrayBuilder = new ArrayBuilder<int>();

            for (var i = 0; i < itemCount; ++i)
            {
                arrayBuilder.AddItem(i);
            }

            FormattingBufferHelper.ClearThenResetIfTooLarge(ref arrayBuilder);

            return arrayBuilder.Capacity;
        }

        [Theory]
        [MemberData(nameof(SmallSizes))]
        public void Should_Keep_Small_Buffer_Stack(int itemCount)
        {
            var capacity = FillAndClearStack(itemCount);

            Assert.True(capacity >= itemCount);
        }

        [Theory]
        [MemberData(nameof(LargeSizes))]
        public void Should_Reset_Large_Buffer_Stack(int itemCount)
        {
            var capacity = FillAndClearStack(itemCount);

            Assert.Equal(0, capacity);
        }

        private static int FillAndClearStack(int itemCount)
        {
            var stack = new Stack<int>();

            for (var i = 0; i < itemCount; ++i)
            {
                stack.Push(i);
            }

            FormattingBufferHelper.ClearThenResetIfTooLarge(stack);

            var array = (Array) stack.GetType()
                .GetField("_array", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(stack)!;

            return array.Length;
        }

        [Theory]
        [MemberData(nameof(SmallSizes))]
        public void Should_Keep_Small_Buffer_Dictionary(int itemCount)
        {
            var capacity = FillAndClearDictionary(itemCount);

            Assert.True(capacity >= itemCount);
        }

        [Theory]
        [MemberData(nameof(LargeSizes))]
        public void Should_Reset_Large_Buffer_Dictionary(int itemCount)
        {
            var capacity = FillAndClearDictionary(itemCount);

            Assert.True(capacity <= 3); // dictionary trims to the nearest prime starting with 3
        }

        private static int FillAndClearDictionary(int itemCount)
        {
            var dictionary = new Dictionary<int, int>();

            for (var i = 0; i < itemCount; ++i)
            {
                dictionary.Add(i, i);
            }

            FormattingBufferHelper.ClearThenResetIfTooLarge(ref dictionary);

            var array = (Array) dictionary.GetType()
                .GetField("_entries", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(dictionary)!;

            return array.Length;
        }
    }
}
