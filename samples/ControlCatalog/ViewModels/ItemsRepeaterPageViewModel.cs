using System.Collections.ObjectModel;
using System.Linq;
using ReactiveUI;

namespace ControlCatalog.ViewModels
{
    public class ItemsRepeaterPageViewModel : ReactiveObject
    {
        private int newItemIndex = 1;

        public ItemsRepeaterPageViewModel()
        {
            Items = new ObservableCollection<string>(
                Enumerable.Range(1, 100000).Select(i => $"Item {i.ToString()}"));
        }

        public ObservableCollection<string> Items { get; }

        public string SelectedItem { get; set; }

        public void AddItem()
        {
            var index = SelectedItem != null ? Items.IndexOf(SelectedItem) : -1;
            Items.Insert(index + 1, $"New Item {newItemIndex++}");
        }
    }
}
