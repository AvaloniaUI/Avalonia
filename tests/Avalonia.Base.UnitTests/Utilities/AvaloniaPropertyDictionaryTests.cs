using System;
using System.Collections.Generic;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class AvaloniaPropertyDictionaryTests
    {
        private static AvaloniaProperty[] TestProperties;

        static AvaloniaPropertyDictionaryTests()
        {
            TestProperties = new AvaloniaProperty[100];

            for (var i = 0; i < 100; ++i)
            {
                TestProperties[i] = new StyledProperty<string>(
                    $"Test{i}",
                    typeof(AvaloniaPropertyDictionaryTests),
                    typeof(AvaloniaPropertyDictionaryTests),
                    new StyledPropertyMetadata<string>());
            }

            Shuffle(TestProperties, 42);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Property_Indexer_Finds_Value(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];
            var result = target[property];

            Assert.Equal($"Value{index}", result);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Property_Indexer_Throws_If_Value_Not_Found(int count)
        {
            var target = CreateTarget(count);
            var index = count;
            var property = TestProperties[index];

            Assert.Throws<KeyNotFoundException>(() => target[property]);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Property_Indexer_Adds_New_Value(int count)
        {
            var target = CreateTarget(count);
            var index = count;
            var property = TestProperties[index];

            target[property] = "new";

            Assert.Equal("new", target[property]);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Property_Indexer_Sets_Existing_Value(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];

            Assert.Equal($"Value{index}", target[property]);

            target[property] = "new";

            Assert.Equal("new", target[property]);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Int_Indexer_Finds_Value(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var result = target[index];

            Assert.NotNull(result);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Int_Indexer_Throws_If_Index_Out_Of_Range(int count)
        {
            var target = CreateTarget(count);
            var index = count;

            Assert.Throws<IndexOutOfRangeException>(() => target[index]);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Add_Adds_New_Value(int count)
        {
            var target = CreateTarget(count);
            var index = count;
            var property = TestProperties[index];

            target.Add(property, "new");

            Assert.Equal("new", target[property]);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Add_Throws_If_Key_Exists(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];

            Assert.Throws<ArgumentException>(() => target.Add(property, "new"));
        }


        [Theory]
        [MemberData(nameof(Counts))]
        public void ContainsKey_Returns_True_If_Value_Exists(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];

            Assert.True(target.ContainsKey(property));
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void ContainsKey_Returns_False_If_Value_Does_Not_Exist(int count)
        {
            var target = CreateTarget(count);
            var index = count;
            var property = TestProperties[index];

            Assert.False(target.ContainsKey(property));
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void GetKeyValue_Finds_Value(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;

            target.GetKeyValue(index, out var property, out var value);

            Assert.NotNull(property);
            Assert.NotNull(value);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void GetKeyValue_Throws_If_Index_Out_Of_Range(int count)
        {
            var target = CreateTarget(count);
            var index = count;

            Assert.Throws<IndexOutOfRangeException>(() => target.GetKeyValue(index, out var _, out var _));
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Remove_Removes_Value(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];

            Assert.True(target.Remove(property));
            Assert.False(target.ContainsKey(property));
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Remove_Returns_False_If_Value_Not_Present(int count)
        {
            var target = CreateTarget(count);
            var index = count;
            var property = TestProperties[index];

            Assert.False(target.Remove(property));
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void Remove_Returns_Existing_Value(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];

            Assert.True(target.Remove(property, out var value));
            Assert.Equal($"Value{index}", value);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void TryAdd_Adds_New_Value(int count)
        {
            var target = CreateTarget(count);
            var index = count;
            var property = TestProperties[index];

            Assert.True(target.TryAdd(property, "new"));

            Assert.Equal("new", target[property]);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void TryAdd_Returns_False_If_Key_Exists(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];

            Assert.False(target.TryAdd(property, "new"));
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void TryGetValue_Finds_Value(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count / 2;
            var property = TestProperties[index];

            Assert.True(target.TryGetValue(property, out var value));
            Assert.Equal($"Value{index}", value);
        }

        [Theory]
        [MemberData(nameof(Counts))]
        public void TryGetValue_Returns_False_If_Key_Does_Not_Exist(int count)
        {
            if (count == 0)
                return;

            var target = CreateTarget(count);
            var index = count;
            var property = TestProperties[index];

            Assert.False(target.TryGetValue(property, out var value));
            Assert.Null(value);
        }

        public static TheoryData<int> Counts()
        {
            var result = new TheoryData<int>();
            result.Add(0);
            result.Add(1);
            result.Add(10);
            result.Add(13);
            result.Add(50);
            result.Add(72);
            return result;
        }

        private static AvaloniaPropertyDictionary<string> CreateTarget(int items)
        {
            var result = new AvaloniaPropertyDictionary<string>();

            for (var i = 0; i < items; ++i)
                result.Add(TestProperties[i], $"Value{i}");

            return result;
        }

        private static void Shuffle<T>(T[] array, int seed)
        {
            var rng = new Random(seed);

            int n = array.Length;
            while (n > 1)
            {
                int k = rng.Next(n--);
                T temp = array[n];
                array[n] = array[k];
                array[k] = temp;
            }
        }
    }
}
