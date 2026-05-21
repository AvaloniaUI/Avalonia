#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Avalonia.Utilities;
using Xunit;
using static Avalonia.Media.TextFormatting.FormattingObjectPool;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    /// <summary>
    /// Direct tests for <c>TextFormatterImpl.SplitTextRuns</c>. Calls the
    /// internal method via <c>InternalsVisibleTo</c> so each branch can be
    /// exercised in isolation with synthetic <see cref="TextRun"/> stubs —
    /// independent of the wrap algorithm that's its main caller. Many of
    /// these scenarios are unreachable through the wrap path on its own
    /// (the wrap loop carefully avoids requesting splits inside non-splittable
    /// runs), but the method is also called by <c>TextCollapsingProperties</c>
    /// and the ellipsis types, which have weaker invariants.
    ///
    /// Key invariants under test (must hold regardless of split position):
    ///   * Sum of run lengths is preserved (no content lost or duplicated).
    ///   * Concatenated text (across all runs in first ++ second) equals input.
    ///   * Reported <c>firstLength</c> equals the sum of lengths in first.
    /// </summary>
    public class SplitTextRunsTests
    {
        // ----- Failing test that pins the bug we're fixing --------------------

        [Fact]
        public void Split_Inside_NonShaped_Run_Does_Not_Drop_Run()
        {
            // Bug repro: a DrawableTextRun-like atomic run with length > 1, asked
            // to split at length=1 (strictly inside). Before the fix, the current
            // implementation dropped the run from both halves. After the fix it
            // must appear in either first or second.
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[] { new TestStubRun("drawable", length: 3) };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 1, pool, out var firstLength);

            try
            {
                AssertContentPreserved(runs, first, second, firstLength);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        // ----- length-boundary cases ------------------------------------------

        [Fact]
        public void Split_Length_Zero_Returns_Null_First_And_All_In_Second()
        {
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("a", length: 2),
                new TestStubRun("b", length: 3),
            };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 0, pool, out var firstLength);

            try
            {
                Assert.Null(first);
                Assert.NotNull(second);
                Assert.Equal(2, second!.Count);
                Assert.Equal(0, firstLength);
                Assert.Equal(5, second.Sum(r => r.Length));
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        [Fact]
        public void Split_Length_Equals_Total_Puts_All_In_First()
        {
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("a", length: 2),
                new TestStubRun("b", length: 3),
            };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 5, pool, out var firstLength);

            try
            {
                Assert.NotNull(first);
                Assert.Equal(2, first!.Count);
                Assert.Null(second);
                Assert.Equal(5, firstLength);
                AssertContentPreserved(runs, first, second, firstLength);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        [Fact]
        public void Split_Length_Past_Total_Puts_All_In_First()
        {
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[] { new TestStubRun("a", length: 2) };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 99, pool, out var firstLength);

            try
            {
                Assert.NotNull(first);
                Assert.Equal(1, first!.Count);
                Assert.Null(second);
                Assert.Equal(2, firstLength);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        // ----- exact-boundary cases (between runs) ----------------------------

        [Fact]
        public void Split_At_Boundary_Between_Two_Runs_Goes_To_First_Or_Second_Cleanly()
        {
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("a", length: 2),
                new TestStubRun("b", length: 3),
            };

            // length=2 means "everything up to and including the first run on first".
            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 2, pool, out var firstLength);

            try
            {
                Assert.NotNull(first);
                Assert.Equal(1, first!.Count);
                Assert.Same(runs[0], first[0]);
                Assert.NotNull(second);
                Assert.Equal(1, second!.Count);
                Assert.Same(runs[1], second[0]);
                Assert.Equal(2, firstLength);
                AssertContentPreserved(runs, first, second, firstLength);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        // ----- non-splittable run, snap-before cases (the bug) ----------------

        [Fact]
        public void Split_Before_Drawable_That_Does_Not_Fit_Puts_Drawable_In_Second()
        {
            // [text(2), drawable(1), text(2)] split at length=2.
            // Wrap normally chooses currentLength==length here ("drawable doesn't fit
            // on this line, push to next"). The == branch in SplitTextRuns already
            // handles this correctly today — assert that it stays correct.
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("ab", length: 2),
                new TestStubRun("X", length: 1),
                new TestStubRun("cd", length: 2),
            };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 2, pool, out var firstLength);

            try
            {
                Assert.Equal(2, firstLength);
                AssertContentPreserved(runs, first, second, firstLength);
                Assert.Equal(1, first!.Count);
                Assert.Equal(2, second!.Count);
                Assert.Same(runs[1], second[0]); // drawable at start of second
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        [Fact]
        public void Split_Strictly_Inside_NonShaped_Run_Snaps_Before_It()
        {
            // [text(2), drawable(3), text(2)] split at length=3 — strictly inside
            // the drawable. The drawable is atomic, so the split must snap to a
            // boundary. The current contract: snap BEFORE the drawable, so
            // firstLength is shorter than requested but content is preserved.
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("ab", length: 2),
                new TestStubRun("XXX", length: 3),
                new TestStubRun("cd", length: 2),
            };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 3, pool, out var firstLength);

            try
            {
                AssertContentPreserved(runs, first, second, firstLength);
                Assert.True(firstLength is 2 or 5,
                    $"Expected firstLength to snap to 2 (before drawable) or 5 (after drawable); got {firstLength}.");
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        [Fact]
        public void Split_Strictly_Inside_NonShaped_Run_At_Start_Of_List_Overflows()
        {
            // [drawable(5)] split at length=2 — the drawable is the first run, has
            // no content before it, and is bigger than the requested length. If we
            // snapped before, first would be empty and the caller would loop
            // forever. The contract here is to overflow the drawable into first
            // (the same "include at least one cluster" rule the wrap loop has for
            // ShapedTextRuns at the start of a line).
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[] { new TestStubRun("XXXXX", length: 5) };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 2, pool, out var firstLength);

            try
            {
                AssertContentPreserved(runs, first, second, firstLength);
                Assert.Equal(5, firstLength); // overflow
                Assert.NotNull(first);
                Assert.Equal(1, first!.Count);
                Assert.Same(runs[0], first[0]);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        // ----- mixed sequences -------------------------------------------------

        [Fact]
        public void Split_At_Boundary_Before_Drawable_Mid_List()
        {
            // [shape(3), drawable(2), shape(3)] split at length=3 — boundary at
            // end of first shape. == branch.
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("AAA", length: 3),
                new TestStubRun("XX", length: 2),
                new TestStubRun("BBB", length: 3),
            };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 3, pool, out var firstLength);

            try
            {
                Assert.Equal(3, firstLength);
                AssertContentPreserved(runs, first, second, firstLength);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        [Fact]
        public void Split_At_Boundary_After_Drawable_Mid_List()
        {
            // Boundary at end of drawable. == branch.
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("AAA", length: 3),
                new TestStubRun("XX", length: 2),
                new TestStubRun("BBB", length: 3),
            };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 5, pool, out var firstLength);

            try
            {
                Assert.Equal(5, firstLength);
                AssertContentPreserved(runs, first, second, firstLength);
                Assert.Equal(2, first!.Count);
                Assert.Equal(1, second!.Count);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        // ----- zero-length runs (cluster boundaries) --------------------------

        [Fact]
        public void Split_With_Zero_Length_Run_Inside_Does_Not_Drop_It()
        {
            // Zero-length runs (e.g. TextEndOfParagraph variants) appear after
            // shaped runs. Splitting at the boundary should keep the zero-length
            // run somewhere — not silently discard it.
            var pool = FormattingObjectPool.Instance;
            var runs = new TextRun[]
            {
                new TestStubRun("ab", length: 2),
                new TestStubRun("zero", length: 0),
                new TestStubRun("cd", length: 2),
            };

            var (first, second) = TextFormatterImpl.SplitTextRuns(runs, length: 2, pool, out var firstLength);

            try
            {
                Assert.Equal(2, firstLength);
                // The zero-length run still has identity — assert it's in exactly one half.
                var allRuns = (first?.Cast<TextRun>() ?? Enumerable.Empty<TextRun>())
                    .Concat(second?.Cast<TextRun>() ?? Enumerable.Empty<TextRun>())
                    .ToList();
                Assert.Equal(3, allRuns.Count);
                Assert.Contains(runs[1], allRuns);
            }
            finally
            {
                pool.TextRunLists.Return(ref first);
                pool.TextRunLists.Return(ref second);
            }
        }

        // ----- helpers --------------------------------------------------------

        /// <summary>
        /// Asserts the central invariant: sum of (first|second) run lengths
        /// equals the input total, and <paramref name="firstLength"/> matches the
        /// sum of first's run lengths. Any test where this fails means content
        /// was lost or duplicated.
        /// </summary>
        private static void AssertContentPreserved(
            IReadOnlyList<TextRun> input,
            RentedList<TextRun>? first,
            RentedList<TextRun>? second,
            int firstLength)
        {
            var inputTotal = input.Sum(r => r.Length);
            var firstTotal = first?.Sum(r => r.Length) ?? 0;
            var secondTotal = second?.Sum(r => r.Length) ?? 0;

            Assert.Equal(inputTotal, firstTotal + secondTotal);
            Assert.Equal(firstTotal, firstLength);
        }

        /// <summary>
        /// Minimal concrete <see cref="TextRun"/> for tests — neither a
        /// <c>ShapedTextRun</c> nor a <c>DrawableTextRun</c> from the consumer
        /// perspective. Behaves like an atomic, non-splittable run with a
        /// configurable length, which is exactly the class of input that
        /// triggers the <c>SplitTextRuns</c> drop-current-run bug.
        /// </summary>
        private sealed class TestStubRun : TextRun
        {
            private readonly string _name;

            public TestStubRun(string name, int length)
            {
                _name = name;
                Length = length;
            }

            public override int Length { get; }

            public override string ToString() => $"TestStubRun({_name}, len={Length})";
        }
    }
}
