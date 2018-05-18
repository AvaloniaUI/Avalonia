using Avalonia.Controls.Presenters;
using Xunit;

namespace Avalonia.Controls.UnitTests.Presenters
{
    public class TextPresenter_Tests
    {
        [Fact]
        public void TextPresenter_Can_Contain_Null_With_Password_Char_Set()
        {
            var target = new TextPresenter
            {
                PasswordChar = '*'
            };

            Assert.NotNull(target.FormattedText);
        }

        [Fact]
        public void TextPresenter_Can_Contain_Null_WithOut_Password_Char_Set()
        {
            var target = new TextPresenter();

            Assert.NotNull(target.FormattedText);
        }

        [Fact]
        public void Text_Presenter_Replaces_Formatted_Text_With_Password_Char()
        {
            var target = new TextPresenter
            {
                PasswordChar = '*',
                Text = "Test"
            };

            Assert.NotNull(target.FormattedText);
            Assert.Equal("****", target.FormattedText.Text);
        }
    }
}
