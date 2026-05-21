using System;
using Avalonia.Media.Fonts.Tables;
using Xunit;

namespace Avalonia.Base.UnitTests.Media.Fonts.Tables
{
    public class DecyclerTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        [InlineData(int.MinValue)]
        public void Constructor_Throws_When_MaxDepth_Is_Less_Than_One(int maxDepth)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Decycler<int>(maxDepth));
        }

        [Fact]
        public void Constructor_Accepts_MaxDepth_Of_One()
        {
            var decycler = new Decycler<int>(maxDepth: 1);

            Assert.Equal(0, decycler.CurrentDepth);
            Assert.Equal(1, decycler.MaxDepth);
        }

        [Fact]
        public void Enter_Increments_CurrentDepth()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            using var guard = decycler.Enter(1);

            Assert.Equal(1, decycler.CurrentDepth);
        }

        [Fact]
        public void Disposing_Guard_Restores_CurrentDepth()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            using (decycler.Enter(1))
            {
                Assert.Equal(1, decycler.CurrentDepth);
            }

            Assert.Equal(0, decycler.CurrentDepth);
        }

        [Fact]
        public void Nested_Enters_Stack_Depth_And_Unwind_In_Reverse()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            using (decycler.Enter(1))
            {
                Assert.Equal(1, decycler.CurrentDepth);

                using (decycler.Enter(2))
                {
                    Assert.Equal(2, decycler.CurrentDepth);

                    using (decycler.Enter(3))
                    {
                        Assert.Equal(3, decycler.CurrentDepth);
                    }

                    Assert.Equal(2, decycler.CurrentDepth);
                }

                Assert.Equal(1, decycler.CurrentDepth);
            }

            Assert.Equal(0, decycler.CurrentDepth);
        }

        [Fact]
        public void Re_Entering_Visited_Id_Throws_CycleDetected()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            using var outer = decycler.Enter(1);

            var ex = Assert.Throws<DecyclerException>(() => decycler.Enter(1));

            Assert.Equal(DecyclerError.CycleDetected, ex.Error);
        }

        [Fact]
        public void Same_Id_Can_Be_Re_Entered_After_Exit()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            using (decycler.Enter(1))
            {
            }

            // The id is no longer in the visited set; re-entering must succeed.
            using var guard = decycler.Enter(1);

            Assert.Equal(1, decycler.CurrentDepth);
        }

        [Fact]
        public void Enter_Beyond_MaxDepth_Throws_DepthLimitExceeded()
        {
            var decycler = new Decycler<int>(maxDepth: 2);

            using var a = decycler.Enter(1);
            using var b = decycler.Enter(2);

            var ex = Assert.Throws<DecyclerException>(() => decycler.Enter(3));

            Assert.Equal(DecyclerError.DepthLimitExceeded, ex.Error);
        }

        [Fact]
        public void DepthLimit_Check_Runs_Before_Cycle_Check()
        {
            // When both conditions could apply (depth is exhausted AND the id is
            // already visited), the depth check fires first. This matters because
            // it means a misconfigured depth cap surfaces as a depth error rather
            // than masquerading as a cycle.
            var decycler = new Decycler<int>(maxDepth: 1);

            using var guard = decycler.Enter(1);

            var ex = Assert.Throws<DecyclerException>(() => decycler.Enter(1));

            Assert.Equal(DecyclerError.DepthLimitExceeded, ex.Error);
        }

        [Fact]
        public void Failed_Enter_Does_Not_Mutate_State()
        {
            var decycler = new Decycler<int>(maxDepth: 1);

            using var guard = decycler.Enter(1);

            Assert.Throws<DecyclerException>(() => decycler.Enter(2));

            // The failed Enter must not have incremented depth or registered the id.
            Assert.Equal(1, decycler.CurrentDepth);
        }

        [Fact]
        public void Reset_Clears_Visited_And_Depth()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            var guard = decycler.Enter(1);
            // Skip the disposal: simulate an abandoned traversal that needs to be
            // cleaned up by Reset (the validator path on the pool).
            _ = guard;

            decycler.Reset();

            Assert.Equal(0, decycler.CurrentDepth);

            // The previously visited id must be enterable again after reset.
            using var fresh = decycler.Enter(1);

            Assert.Equal(1, decycler.CurrentDepth);
        }

        [Fact]
        public void Reset_Is_Safe_To_Call_On_Empty_Decycler()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            decycler.Reset();
            decycler.Reset();

            Assert.Equal(0, decycler.CurrentDepth);
        }

        [Fact]
        public void Guard_Dispose_Is_Idempotent()
        {
            var decycler = new Decycler<int>(maxDepth: 4);

            var guard = decycler.Enter(1);

            guard.Dispose();
            guard.Dispose(); // second call must not double-decrement.

            Assert.Equal(0, decycler.CurrentDepth);

            // Depth must not be negative; entering a new node still works.
            using var next = decycler.Enter(2);

            Assert.Equal(1, decycler.CurrentDepth);
        }

        [Fact]
        public void MaxDepth_Property_Reflects_Constructor_Argument()
        {
            var decycler = new Decycler<int>(maxDepth: 17);

            Assert.Equal(17, decycler.MaxDepth);
        }

        [Fact]
        public void DecyclerException_Carries_Error_Code_And_Message()
        {
            var ex = new DecyclerException(DecyclerError.CycleDetected, "boom");

            Assert.Equal(DecyclerError.CycleDetected, ex.Error);
            Assert.Equal("boom", ex.Message);
        }

        [Fact]
        public void Works_With_Other_Struct_Types()
        {
            // Decycler<T> is constrained to struct; the typical instantiations are
            // int (composite-glyph ids) and ushort (paint-graph glyph ids). Verify
            // ushort works end-to-end.
            var decycler = new Decycler<ushort>(maxDepth: 4);

            using (decycler.Enter(1))
            {
                Assert.Throws<DecyclerException>(() => decycler.Enter((ushort)1));
            }

            Assert.Equal(0, decycler.CurrentDepth);
        }
    }
}
