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
            target.Items = new ObservableCollection<string>();
            target.Items = new ObservableCollection<string>();
        }

        [Fact]
        public void Can_Reassign_Items_To_Null()
        {
            var target = new ItemsRepeater();
            target.Items = new ObservableCollection<string>();
            target.Items = null;
        }
    }
}
