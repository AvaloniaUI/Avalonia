using Avalonia.Markup.Data;
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

        [Fact]
        public void RadioButton_In_Same_Group_Is_Unchecked()
        {
            var parent = new Panel();

            var panel1 = new Panel();
            var panel2 = new Panel();

            parent.Children.Add(panel1);
            parent.Children.Add(panel2);

            var radioButton1 = new RadioButton();
            radioButton1.GroupName = "A";
            radioButton1.IsChecked = false;

            var radioButton2 = new RadioButton();
            radioButton2.GroupName = "A";
            radioButton2.IsChecked = true;

            var radioButton3 = new RadioButton();
            radioButton3.GroupName = "A";
            radioButton3.IsChecked = false;

            panel1.Children.Add(radioButton1);
            panel1.Children.Add(radioButton2);
            panel2.Children.Add(radioButton3);

            Assert.False(radioButton1.IsChecked);
            Assert.True(radioButton2.IsChecked);
            Assert.False(radioButton3.IsChecked);

            radioButton3.IsChecked = true;

            Assert.False(radioButton1.IsChecked);
            Assert.False(radioButton2.IsChecked);
            Assert.True(radioButton3.IsChecked);
        }

        [Fact]
        public void RadioButton_Empty_GroupName_Not_Influence_Other_Groups()
        {
            var parent = new Panel();
            
            var radioButton1 = new RadioButton();
            radioButton1.GroupName = "A";
            radioButton1.IsChecked = true;
            var radioButton2 = new RadioButton();
            radioButton2.GroupName = "A";
            radioButton2.IsChecked = false;

            var radioButton3 = new RadioButton();
            radioButton3.GroupName = null;
            radioButton3.IsChecked = false;
            var radioButton4 = new RadioButton();
            radioButton4.GroupName = null;
            radioButton4.IsChecked = true;

            parent.Children.Add(radioButton1); 
            parent.Children.Add(radioButton2); 
            parent.Children.Add(radioButton3);
            parent.Children.Add(radioButton4);

            Assert.True(radioButton1.IsChecked);
            Assert.False(radioButton2.IsChecked);
            Assert.False(radioButton3.IsChecked);
            Assert.True(radioButton4.IsChecked);

            radioButton3.IsChecked = true;

            Assert.True(radioButton1.IsChecked);
            Assert.False(radioButton2.IsChecked);
            Assert.True(radioButton3.IsChecked);
            Assert.False(radioButton4.IsChecked);


        }
    }
}
