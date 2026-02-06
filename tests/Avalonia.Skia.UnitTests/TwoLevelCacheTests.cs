#nullable enable

using System;
using System.Collections.Generic;
using Avalonia.Skia;
using Xunit;

namespace Avalonia.Skia.UnitTests
{
    public class TwoLevelCacheTests
    {
        [Fact]
        public void Constructor_WithNegativeSecondarySize_ThrowsArgumentOutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new TwoLevelCache<string, object>(-1));
        }

        [Fact]
        public void Constructor_WithZeroSecondarySize_DoesNotThrow()
        {
            var cache = new TwoLevelCache<string, object>(0);
            Assert.NotNull(cache);
        }

        [Fact]
        public void TryGet_EmptyCache_ReturnsFalse()
        {
            var cache = new TwoLevelCache<string, object>();

            var result = cache.TryGet("key", out var value);

            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void GetOrAdd_FirstItem_StoresInPrimary()
        {
            var cache = new TwoLevelCache<string, object>();
            var value = new object();

            var result = cache.GetOrAdd("key1", _ => value);

            Assert.Same(value, result);
            Assert.True(cache.TryGet("key1", out var retrieved));
            Assert.Same(value, retrieved);
        }

        [Fact]
        public void GetOrAdd_SameKey_ReturnsExistingValue()
        {
            var cache = new TwoLevelCache<string, object>();
            var value1 = new object();
            var value2 = new object();

            cache.GetOrAdd("key", _ => value1);
            var result = cache.GetOrAdd("key", _ => value2);

            Assert.Same(value1, result);
        }

        [Fact]
        public void GetOrAdd_SecondItem_StoresInSecondary()
        {
            var cache = new TwoLevelCache<string, object>(secondarySize: 3);
            var value1 = new object();
            var value2 = new object();

            cache.GetOrAdd("key1", _ => value1);
            cache.GetOrAdd("key2", _ => value2);

            Assert.True(cache.TryGet("key1", out var retrieved1));
            Assert.Same(value1, retrieved1);
            Assert.True(cache.TryGet("key2", out var retrieved2));
            Assert.Same(value2, retrieved2);
        }

        [Fact]
        public void GetOrAdd_MultipleItems_StoresCorrectly()
        {
            var cache = new TwoLevelCache<string, object>(secondarySize: 3);
            var values = new object[4];
            for (int i = 0; i < 4; i++)
            {
                values[i] = new object();
                cache.GetOrAdd($"key{i}", _ => values[i]);
            }

            // All should be retrievable
            for (int i = 0; i < 4; i++)
            {
                Assert.True(cache.TryGet($"key{i}", out var retrieved));
                Assert.Same(values[i], retrieved);
            }
        }

        [Fact]
        public void GetOrAdd_ExceedsCapacity_CallsEvictionAction()
        {
            var evictedValues = new List<object?>();
            var cache = new TwoLevelCache<string, object>(
                secondarySize: 2,
                evictionAction: v => evictedValues.Add(v));

            var value1 = new object();
            var value2 = new object();
            var value3 = new object();
            var value4 = new object();

            cache.GetOrAdd("key1", _ => value1);
            cache.GetOrAdd("key2", _ => value2);
            cache.GetOrAdd("key3", _ => value3);

            // No evictions yet
            Assert.Empty(evictedValues);

            // This should cause eviction
            cache.GetOrAdd("key4", _ => value4);

            Assert.Single(evictedValues);
            Assert.Same(value2, evictedValues[0]);
        }

        [Fact]
        public void GetOrAdd_ZeroSecondarySize_EvictsPrimaryImmediately()
        {
            var evictedValues = new List<object?>();
            var cache = new TwoLevelCache<string, object>(
                secondarySize: 0,
                evictionAction: v => evictedValues.Add(v));

            var value1 = new object();
            var value2 = new object();

            cache.GetOrAdd("key1", _ => value1);
            cache.GetOrAdd("key2", _ => value2);

            Assert.Single(evictedValues);
            Assert.Same(value1, evictedValues[0]);

            // Only the latest value should be retrievable
            Assert.False(cache.TryGet("key1", out _));
            Assert.True(cache.TryGet("key2", out var retrieved));
            Assert.Same(value2, retrieved);
        }

        [Fact]
        public void GetOrAdd_DuplicateKey_ReturnsExistingWithoutCallingFactory()
        {
            var cache = new TwoLevelCache<string, object>();
            var value1 = new object();
            var factoryCalled = false;

            // Add initial value
            cache.GetOrAdd("key", _ => value1);

            // Try to add again - factory should not be called
            var result = cache.GetOrAdd("key", _ =>
            {
                factoryCalled = true;
                return new object();
            });

            // Should return first value without calling factory
            Assert.Same(value1, result);
            Assert.False(factoryCalled);
        }

        [Fact]
        public void GetOrAdd_DuplicateKeyInSecondary_ReturnsExistingWithoutCallingFactory()
        {
            var cache = new TwoLevelCache<string, object>(secondarySize: 2);
            var value1 = new object();
            var value2 = new object();
            var factoryCalled = false;

            cache.GetOrAdd("key1", _ => value1);
            cache.GetOrAdd("key2", _ => value2);

            // Try to add key2 again - factory should not be called
            var result = cache.GetOrAdd("key2", _ =>
            {
                factoryCalled = true;
                return new object();
            });

            Assert.Same(value2, result);
            Assert.False(factoryCalled);
        }

        [Fact]
        public void ClearAndDispose_EmptyCache_DoesNotThrow()
        {
            var cache = new TwoLevelCache<string, object>();
            cache.ClearAndDispose();
        }

        [Fact]
        public void ClearAndDispose_WithValues_CallsEvictionActionForAll()
        {
            var evictedValues = new List<object?>();
            var cache = new TwoLevelCache<string, object>(
                secondarySize: 2,
                evictionAction: v => evictedValues.Add(v));

            var value1 = new object();
            var value2 = new object();
            var value3 = new object();

            cache.GetOrAdd("key1", _ => value1);
            cache.GetOrAdd("key2", _ => value2);
            cache.GetOrAdd("key3", _ => value3);

            cache.ClearAndDispose();

            Assert.Equal(3, evictedValues.Count);
            Assert.Contains(value1, evictedValues);
            Assert.Contains(value2, evictedValues);
            Assert.Contains(value3, evictedValues);
        }

        [Fact]
        public void ClearAndDispose_ClearsAllEntries()
        {
            var cache = new TwoLevelCache<string, object>(secondarySize: 2);

            cache.GetOrAdd("key1", _ => new object());
            cache.GetOrAdd("key2", _ => new object());
            cache.ClearAndDispose();

            Assert.False(cache.TryGet("key1", out _));
            Assert.False(cache.TryGet("key2", out _));
        }

        [Fact]
        public void GetOrAdd_WithCustomComparer_UsesComparer()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var cache = new TwoLevelCache<string, object>(comparer: comparer);

            var value = new object();
            cache.GetOrAdd("KEY", _ => value);

            Assert.True(cache.TryGet("key", out var retrieved));
            Assert.Same(value, retrieved);
        }

        [Fact]
        public void TryGet_WithCustomComparer_UsesComparer()
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            var cache = new TwoLevelCache<string, object>(
                secondarySize: 2,
                comparer: comparer);

            var value1 = new object();
            var value2 = new object();

            cache.GetOrAdd("PRIMARY", _ => value1);
            cache.GetOrAdd("SECONDARY", _ => value2);

            Assert.True(cache.TryGet("primary", out var retrieved1));
            Assert.Same(value1, retrieved1);
            Assert.True(cache.TryGet("secondary", out var retrieved2));
            Assert.Same(value2, retrieved2);
        }

        [Fact]
        public void GetOrAdd_IntKeys_WorksCorrectly()
        {
            var cache = new TwoLevelCache<int, object>(secondarySize: 2);

            var value1 = new object();
            var value2 = new object();
            var value3 = new object();

            cache.GetOrAdd(1, _ => value1);
            cache.GetOrAdd(2, _ => value2);
            cache.GetOrAdd(3, _ => value3);

            Assert.True(cache.TryGet(1, out var retrieved1));
            Assert.Same(value1, retrieved1);
            Assert.True(cache.TryGet(2, out var retrieved2));
            Assert.Same(value2, retrieved2);
            Assert.True(cache.TryGet(3, out var retrieved3));
            Assert.Same(value3, retrieved3);
        }

        [Fact]
        public void GetOrAdd_RotatesSecondaryCorrectly()
        {
            var evictedValues = new List<object?>();
            var cache = new TwoLevelCache<int, object>(
                secondarySize: 2,
                evictionAction: v => evictedValues.Add(v));

            var values = new object[5];
            for (int i = 0; i < 5; i++)
            {
                values[i] = new object();
                cache.GetOrAdd(i, _ => values[i]);
            }

            // Primary: 0, Secondary: [1, 2]
            // After adding 3: Primary: 0, Secondary: [3, 1] (evicts 2)
            // After adding 4: Primary: 0, Secondary: [4, 3] (evicts 1)

            Assert.Equal(2, evictedValues.Count);
            Assert.Contains(values[2], evictedValues);
            Assert.Contains(values[1], evictedValues);

            // These should still be in cache
            Assert.True(cache.TryGet(0, out _));
            Assert.True(cache.TryGet(3, out _));
            Assert.True(cache.TryGet(4, out _));

            // These should be evicted
            Assert.False(cache.TryGet(1, out _));
            Assert.False(cache.TryGet(2, out _));
        }

        [Fact]
        public void FactoryFunction_ReceivesCorrectKey()
        {
            var cache = new TwoLevelCache<string, string>();
            string? capturedKey = null;

            cache.GetOrAdd("testKey", key =>
            {
                capturedKey = key;
                return "value";
            });

            Assert.Equal("testKey", capturedKey);
        }

        [Fact]
        public void GetOrAdd_NullEvictionAction_DoesNotThrow()
        {
            var cache = new TwoLevelCache<string, object>(
                secondarySize: 1,
                evictionAction: null);

            cache.GetOrAdd("key1", _ => new object());
            cache.GetOrAdd("key2", _ => new object());
            cache.GetOrAdd("key3", _ => new object()); // Should evict without error

            cache.ClearAndDispose(); // Should also not throw
        }
    }
}
