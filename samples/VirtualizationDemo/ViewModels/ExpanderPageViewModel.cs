using System.Collections.ObjectModel;
using System.Linq;

namespace VirtualizationDemo.ViewModels;

internal class ExpanderPageViewModel
{
    public ExpanderPageViewModel()
    {
        Items = new(Enumerable.Range(0, 100).Select(x => new  ExpanderItemViewModel
        {
            Header = $"Item {x}",
        }));
    }

    public ObservableCollection<ExpanderItemViewModel> Items { get; set; }
}
