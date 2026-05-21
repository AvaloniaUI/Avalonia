#nullable enable

using System.Linq;
using System.Reflection;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using Xunit;

namespace Avalonia.Skia.UnitTests.Media.TextFormatting
{
    /// <summary>
    /// <see cref="ShapedBuffer"/> rents its glyph storage from
    /// <c>ArrayPool&lt;GlyphInfo&gt;.Shared</c> and <see cref="ShapedBuffer.Split"/> /
    /// <see cref="ShapedBuffer.WithBidiLevel"/> produce child buffers that view into
    /// the parent's pool-rented array via <c>ArraySlice</c>. Without a shared
    /// ownership refcount, disposing the parent (which
    /// <see cref="TextFormatterImpl.SplitTextRuns"/> does unconditionally) returns
    /// the pool array while the children still reference it; later pool consumers
    /// can then corrupt those children's glyph data.
    ///
    /// The tests below assert the ownership invariant directly via reflection
    /// on the owner's private <c>_rentedBuffer</c> field — that's deterministic
    /// regardless of whether the pool happens to re-rent the same array, and it
    /// pins the contract the fix introduces: the pool array survives as long as
    /// any view into it is still alive.
    /// </summary>
    public class ShapedBufferLifetimeTests
    {
        private static readonly FieldInfo s_rentedBufferField =
            typeof(ShapedBuffer).GetField("_rentedBuffer", BindingFlags.NonPublic | BindingFlags.Instance)!;

        [Fact]
        public void Split_Children_Keep_Pool_Array_Alive_Until_All_Disposed()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "Hello world abcdef";
                var ownerRun = BuildShapedRun(text, FlowDirection.LeftToRight);
                var ownerBuffer = ownerRun.ShapedBuffer;

                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                var split = ownerRun.Split(text.Length / 2);
                Assert.NotNull(split.First);
                Assert.NotNull(split.Second);

                // Mimic TextFormatterImpl.SplitTextRuns: dispose the owner
                // immediately after splitting. The fix's contract: while child
                // views still reference the rented array, it must NOT be
                // returned to the pool.
                ownerRun.Dispose();
                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                split.First!.Dispose();
                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                split.Second!.Dispose();
                // Last reference released — now the pool array is returned.
                Assert.Null(GetRentedBuffer(ownerBuffer));
            }
        }

        [Fact]
        public void Split_Children_Keep_Pool_Array_Alive_Rtl()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "السلام عليكم ورحمة الله وبركاته";
                var ownerRun = BuildShapedRun(text, FlowDirection.RightToLeft);
                var ownerBuffer = ownerRun.ShapedBuffer;
                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                var split = ownerRun.Split(text.Length / 2);
                Assert.NotNull(split.First);
                Assert.NotNull(split.Second);

                ownerRun.Dispose();
                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                split.First!.Dispose();
                split.Second!.Dispose();
                Assert.Null(GetRentedBuffer(ownerBuffer));
            }
        }

        [Fact]
        public void Chained_Splits_All_Share_Owner_Refcount()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "Hello world abcdef ghijkl";
                var ownerRun = BuildShapedRun(text, FlowDirection.LeftToRight);
                var ownerBuffer = ownerRun.ShapedBuffer;

                // Split, then split a child again. The grandchild must also
                // keep the original owner's pool array alive.
                var split1 = ownerRun.Split(text.Length / 2);
                Assert.NotNull(split1.First);
                Assert.NotNull(split1.Second);

                var split2 = split1.Second!.Split(4);
                Assert.NotNull(split2.First);
                Assert.NotNull(split2.Second);

                ownerRun.Dispose();
                split1.First!.Dispose();
                split1.Second!.Dispose();
                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                split2.First!.Dispose();
                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                split2.Second!.Dispose();
                Assert.Null(GetRentedBuffer(ownerBuffer));
            }
        }

        [Fact]
        public void WithBidiLevel_View_Keeps_Pool_Array_Alive()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "aaa bbb";
                var ownerRun = BuildShapedRun(text, FlowDirection.LeftToRight);
                var ownerBuffer = ownerRun.ShapedBuffer;

                // WithBidiLevel returns `this` when the level matches; flip to
                // ensure a fresh view is created (which is what trips the bug).
                var differentLevel = (sbyte)(ownerBuffer.BidiLevel == 0 ? 1 : 0);
                var viewBuffer = ownerBuffer.WithBidiLevel(differentLevel);
                Assert.NotSame(ownerBuffer, viewBuffer);

                ownerRun.Dispose();
                Assert.NotNull(GetRentedBuffer(ownerBuffer));

                viewBuffer.Dispose();
                Assert.Null(GetRentedBuffer(ownerBuffer));
            }
        }

        [Fact]
        public void Double_Dispose_On_Owner_Is_Safe()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "abcdef";
                var run = BuildShapedRun(text, FlowDirection.LeftToRight);
                run.Dispose();
                // Without an idempotent dispose we'd double-return the pool
                // array, corrupting ArrayPool state.
                run.Dispose();
            }
        }

        [Fact]
        public void Double_Dispose_On_Split_Child_Is_Safe()
        {
            using (TextFormatterTests.Start())
            {
                const string text = "abcdef";
                var run = BuildShapedRun(text, FlowDirection.LeftToRight);

                var split = run.Split(3);
                Assert.NotNull(split.First);
                Assert.NotNull(split.Second);

                split.First!.Dispose();
                split.First!.Dispose();
                split.Second!.Dispose();
                run.Dispose();
            }
        }

        // -- helpers --------------------------------------------------------

        private static ShapedTextRun BuildShapedRun(string text, FlowDirection flow)
        {
            var props = new GenericTextRunProperties(Typeface.Default, 12, foregroundBrush: Brushes.Black);
            var paragraphProps = new GenericTextParagraphProperties(
                flow, TextAlignment.Left, true, true, props, TextWrapping.NoWrap, 0, 0, 0);
            var source = new SingleBufferTextSource(text, props);
            var formatter = new TextFormatterImpl();
            var line = formatter.FormatLine(source, 0, double.PositiveInfinity, paragraphProps);
            Assert.NotNull(line);
            var run = line!.TextRuns.OfType<ShapedTextRun>().FirstOrDefault();
            Assert.NotNull(run);
            // Note: managed object scope doesn't auto-dispose, so we don't need
            // to AddRef — the returned ShapedTextRun starts at refcount=1 and
            // the test controls disposal explicitly.
            return run!;
        }

        private static object? GetRentedBuffer(ShapedBuffer buffer)
            => s_rentedBufferField.GetValue(buffer);
    }
}
