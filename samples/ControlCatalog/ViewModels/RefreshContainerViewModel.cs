using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using ControlCatalog.Pages;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class RefreshContainerViewModel : ViewModelBase
    {
        public ObservableCollection<string> Items { get; }

        public RefreshContainerViewModel()
        {
            Items = new ObservableCollection<string>(Enumerable.Range(1, 200).Select(i => $"Item {i}"));
        }

        public async Task AddToTop()
        {
            await Task.Delay(3000);
            Items.Insert(0, $"Item {200 - Items.Count}");
        }
    }
}
