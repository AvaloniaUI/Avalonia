using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class LabelTests : ScopedTestBase
    {
        [Fact]
        public void Label_LetterSpacing_Default_Value_Is_Zero()
        {
            var label = new Label();
            Assert.Equal(0, label.LetterSpacing);
        }

        [Fact]
        public void Label_LetterSpacing_Can_Be_Set_And_Retrieved()
        {
            var label = new Label { LetterSpacing = 2.5 };
            Assert.Equal(2.5, label.LetterSpacing);
        }

        [Fact]
        public void Label_LetterSpacing_Inherits_From_TemplatedControl()
        {
            var label = new Label { LetterSpacing = 3.0 };
            // LetterSpacing is inherited from TemplatedControl
            Assert.Equal(3.0, label.LetterSpacing);
        }

        [Fact]
        public void Label_LetterSpacing_Can_Be_Negative()
        {
            var label = new Label { LetterSpacing = -1.5 };
            Assert.Equal(-1.5, label.LetterSpacing);
        }
    }
}
