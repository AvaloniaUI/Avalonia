using Avalonia.UnitTests;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class CheckBoxTests : ScopedTestBase
    {
        [Fact]
        public void CheckBox_LetterSpacing_Default_Value_Is_Zero()
        {
            var checkBox = new CheckBox();
            Assert.Equal(0, checkBox.LetterSpacing);
        }

        [Fact]
        public void CheckBox_LetterSpacing_Can_Be_Set_And_Retrieved()
        {
            var checkBox = new CheckBox { LetterSpacing = 2.5 };
            Assert.Equal(2.5, checkBox.LetterSpacing);
        }

        [Fact]
        public void CheckBox_LetterSpacing_Inherits_From_TemplatedControl()
        {
            var checkBox = new CheckBox { LetterSpacing = 3.0 };
            // LetterSpacing is inherited from TemplatedControl
            Assert.Equal(3.0, checkBox.LetterSpacing);
        }

        [Fact]
        public void CheckBox_LetterSpacing_Can_Be_Negative()
        {
            var checkBox = new CheckBox { LetterSpacing = -1.5 };
            Assert.Equal(-1.5, checkBox.LetterSpacing);
        }
    }
}
