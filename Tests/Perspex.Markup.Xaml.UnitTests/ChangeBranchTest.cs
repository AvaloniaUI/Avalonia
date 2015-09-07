namespace Perspex.Xaml.Base.UnitTest
{
    using System;
    using Markup.Xaml.DataBinding.ChangeTracking;
    using SampleModel;
    using Xunit;

    public class ChangeBranchTest
    {
        [Fact]
        public void GetValueOfMemberOfStruct()
        {
            var level1 = new Level1();
            level1.DateTime = new DateTime(1, 2, 3, 4, 5, 6);

            var branch = new ObservablePropertyBranch(level1, new PropertyPath("DateTime.Minute"));

            var day = branch.Value;
            Assert.Equal(day, branch.Value);
        }

        [Fact]
        public void OnePathOnly()
        {
            var level1 = new Level1();
            
            var branch = new ObservablePropertyBranch(level1, new PropertyPath("Text"));
            var newValue = "Hey now";
            branch.Value = newValue;
            
            Assert.Equal(level1.Text, newValue);
        }

        [Fact]
        public void SettingValueToUnderlyingProperty_ChangesTheValueInBranch()
        {
            var level1 = new Level1();

            level1.Level2.Level3.Property = 3;

            var branch = new ObservablePropertyBranch(level1, new PropertyPath("Level2.Level3.Property"));
            Assert.Equal(3, branch.Value);
        }

        [Fact]
        public void SettingValueToBranch_ChangesTheUnderlyingProperty()
        {
            var level1 = new Level1();

            var branch = new ObservablePropertyBranch(level1, new PropertyPath("Level2.Level3.Property"));
            branch.Value = 3;
            Assert.Equal(3, level1.Level2.Level3.Property);
        }

        [Fact]
        public void SettingValueProperty_RaisesChangeInBranch()
        {
            var level1 = new Level1();

            var branch = new ObservablePropertyBranch(level1, new PropertyPath("Level2.Level3.Property"));
            bool received = false;
            ObservableExtensions.Subscribe(branch.Values, v => received = ((int)v == 3));

            level1.Level2.Level3.Property = 3;

            Assert.True(received);
        }
    }
}
