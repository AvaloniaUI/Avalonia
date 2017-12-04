using Avalonia.Markup.Xaml.Data;
using Avalonia.UnitTests;

using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class RadioButtonTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Indeterminate_RadioButton_Is_Not_Unchecked_After_Checking_Other_Radio_Button(bool isThreeState)
        {
            var panel = new Panel();

            var radioButton1 = new RadioButton();
            radioButton1.IsThreeState = false;
            radioButton1.IsChecked = false;

            var radioButton2 = new RadioButton();
            radioButton2.IsThreeState = isThreeState;
            radioButton2.IsChecked = null;

            panel.Children.Add(radioButton1);
            panel.Children.Add(radioButton2);

            Assert.Null(radioButton2.IsChecked);

            radioButton1.IsChecked = true;

            Assert.True(radioButton1.IsChecked);
            Assert.Null(radioButton2.IsChecked);
        }
    }
}
