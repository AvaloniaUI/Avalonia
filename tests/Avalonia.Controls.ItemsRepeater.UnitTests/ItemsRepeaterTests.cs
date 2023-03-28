using System.Collections.ObjectModel;
using Xunit;

namespace Avalonia.Controls.UnitTests
{
    public class ItemsRepeaterTests
    {
        [Fact]
        public void Can_Reassign_Items()
        {
            var target = new ItemsRepeater();
            target.ItemsSource = new ObservableCollection<string>();
            target.ItemsSource = new ObservableCollection<string>();
        }

        [Fact]
        public void Can_Reassign_Items_To_Null()
        {
            var target = new ItemsRepeater();
            target.ItemsSource = new ObservableCollection<string>();
            target.ItemsSource = null;
        }
    }
}
