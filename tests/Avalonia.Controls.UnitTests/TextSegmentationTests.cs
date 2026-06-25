using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class TextSegmentationTests : ScopedTestBase
    {
        [Theory]
        [InlineData(0x000A)] // LF
        [InlineData(0x000D)] // CR
        [InlineData(0x0085)] // NEL  (Next Line)
        [InlineData(0x000B)] // VT   (Vertical Tab)
        [InlineData(0x000C)] // FF   (Form Feed)
        [InlineData(0x2028)] // LINE SEPARATOR
        [InlineData(0x2029)] // PARAGRAPH SEPARATOR
        public void LineBounds_Splits_On_Each_Uax14_Mandatory_Break(int separatorCode)
        {
            var text = "ab" + (char)separatorCode + "cd";

            // The break character bounds the line on either side and is excluded from the content.
            Assert.Equal((0, 2), TextSegmentation.LineBounds(0, text));
            Assert.Equal((3, 5), TextSegmentation.LineBounds(3, text));
        }

        [Fact]
        public void LineBounds_Does_Not_Break_On_A_Soft_Wrap_Opportunity()
        {
            // A space is a UAX-14 break *opportunity*, not a mandatory break: the logical line spans it.
            var text = "ab cd";

            Assert.Equal((0, 5), TextSegmentation.LineBounds(0, text));
            Assert.Equal((0, 5), TextSegmentation.LineBounds(4, text));
        }

        [Fact]
        public void LineBounds_Treats_Crlf_As_A_Single_Break()
        {
            var text = "ab" + (char)0x000D + (char)0x000A + "cd";

            // Content carets resolve to their own line, terminator excluded.
            Assert.Equal((0, 2), TextSegmentation.LineBounds(0, text));
            Assert.Equal((4, 6), TextSegmentation.LineBounds(4, text));

            // A caret on either half of the CR/LF pair belongs to the first line - CRLF is one break
            // (UAX-14 LB5), not two with an empty line between them.
            Assert.Equal((0, 2), TextSegmentation.LineBounds(2, text)); // on CR
            Assert.Equal((0, 2), TextSegmentation.LineBounds(3, text)); // on LF
        }
    }
}
