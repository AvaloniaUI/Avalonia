using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Utilities
{
    public class ObjectPoolTests
    {
        private sealed class Item
        {
            public int State;
        }

        [Fact]
        public void Constructor_Throws_When_Factory_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(() => new ObjectPool<Item>(factory: null!));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Constructor_Throws_When_MaxSize_Is_Less_Than_One(int maxSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ObjectPool<Item>(() => new Item(), maxSize: maxSize));
        }

        [Fact]
        public void Constructor_Accepts_MaxSize_Of_One()
        {
            var pool = new ObjectPool<Item>(() => new Item(), maxSize: 1);

            var item = pool.Rent();

            Assert.NotNull(item);
        }

        [Fact]
        public void Rent_Creates_New_Item_When_Pool_Is_Empty()
        {
            var pool = new ObjectPool<Item>(() => new Item());

            var first = pool.Rent();
            var second = pool.Rent();

            Assert.NotNull(first);
            Assert.NotNull(second);
            Assert.NotSame(first, second);
        }

        [Fact]
        public void Returned_Item_Is_Reused_By_Subsequent_Rent()
        {
            var pool = new ObjectPool<Item>(() => new Item());

            var item = pool.Rent();
            pool.Return(item);

            var rented = pool.Rent();

            Assert.Same(item, rented);
        }

        [Fact]
        public void Return_Ignores_Null()
        {
            var pool = new ObjectPool<Item>(() => new Item());

            pool.Return(null!);

            // Pool stays empty, so the next Rent must produce a fresh item.
            var item = pool.Rent();

            Assert.NotNull(item);
        }

        [Fact]
        public void Return_Drops_Item_When_Pool_Is_Full()
        {
            var pool = new ObjectPool<Item>(() => new Item(), maxSize: 2);

            var a = new Item();
            var b = new Item();
            var c = new Item();

            pool.Return(a);
            pool.Return(b);
            pool.Return(c); // pool full -> dropped

            var rented = new HashSet<Item>
            {
                pool.Rent(),
                pool.Rent(),
            };

            // The two rented items must come from {a, b}; c was dropped.
            Assert.Contains(a, rented);
            Assert.Contains(b, rented);
            Assert.DoesNotContain(c, rented);
        }

        [Fact]
        public void Validator_Is_Invoked_On_Return()
        {
            var validatorCalls = 0;
            var pool = new ObjectPool<Item>(
                factory: () => new Item(),
                validator: _ =>
                {
                    validatorCalls++;
                    return true;
                });

            var item = pool.Rent();
            pool.Return(item);

            Assert.Equal(1, validatorCalls);
        }

        [Fact]
        public void Validator_Is_Not_Invoked_On_Rent()
        {
            // The validator's job is to prepare an item for re-use *before* it goes back
            // into the pool. Running it on Rent would either duplicate the work or imply
            // a different contract (validate-on-take). Pin the current contract.
            var validatorCalls = 0;
            var pool = new ObjectPool<Item>(
                factory: () => new Item(),
                validator: _ =>
                {
                    validatorCalls++;
                    return true;
                });

            _ = pool.Rent();           // fresh from factory; validator must not run
            var item = pool.Rent();
            pool.Return(item);          // one validator call here
            _ = pool.Rent();           // pulled from pool; validator must not run again

            Assert.Equal(1, validatorCalls);
        }

        [Fact]
        public void Return_Drops_Item_When_Validator_Returns_False()
        {
            var pool = new ObjectPool<Item>(
                factory: () => new Item(),
                validator: _ => false);

            var item = pool.Rent();
            pool.Return(item);

            // Validator rejected the item, so the pool is empty and the next
            // Rent produces a fresh instance.
            var rented = pool.Rent();

            Assert.NotSame(item, rented);
        }

        [Fact]
        public void Validator_Can_Reset_Item_State_Before_Pooling()
        {
            var pool = new ObjectPool<Item>(
                factory: () => new Item(),
                validator: i =>
                {
                    i.State = 0;
                    return true;
                });

            var item = pool.Rent();
            item.State = 42;
            pool.Return(item);

            var rented = pool.Rent();

            Assert.Same(item, rented);
            Assert.Equal(0, rented.State);
        }

        [Fact]
        public void Pool_Stays_Within_MaxSize_Under_Concurrent_Returns()
        {
            const int maxSize = 8;
            const int returnsPerThread = 100;
            const int threadCount = 16;

            var pool = new ObjectPool<Item>(() => new Item(), maxSize: maxSize);

            Parallel.For(0, threadCount, _ =>
            {
                for (var i = 0; i < returnsPerThread; i++)
                {
                    pool.Return(new Item { State = 1 });
                }
            });

            // Drain the pool. Items that came from the pool will still have State==1;
            // factory-created items will have the default State==0.
            var pooled = 0;
            var seen = new HashSet<Item>();

            for (var i = 0; i < maxSize * 2; i++)
            {
                var item = pool.Rent();
                if (!seen.Add(item))
                {
                    Assert.Fail("ObjectPool returned the same instance twice without an intervening Return.");
                }

                if (item.State == 1)
                {
                    pooled++;
                }
            }

            Assert.True(pooled <= maxSize,
                $"Expected to observe at most {maxSize} pooled items, observed {pooled}.");

        [Fact]
        public void Rent_And_Return_Survive_Parallel_Use_Without_Losing_Or_Duplicating_Items()
        {
            const int iterations = 5_000;
            const int threadCount = 8;

            var pool = new ObjectPool<Item>(() => new Item(), maxSize: 32);

            Parallel.For(0, threadCount, _ =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    var item = pool.Rent();
                    Assert.NotNull(item);
                    pool.Return(item);
                }
            });
        }
    }
}
