using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog.Pages
{
    public class ListBoxBugRepro : UserControl
    {
        public ListBoxBugRepro()
        {
            InitializeComponent();
            DataContext = new PageViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private class PageViewModel : ReactiveObject
        {

            private ObservableCollection<string> _list = new ObservableCollection<string>()
            {
                "Foo",
                "Bar"
            };

            public ObservableCollection<string> List
            {
                get => _list;
                set
                {
                    this.RaiseAndSetIfChanged(ref _list, value);
                }
            }

            public void RemoveFirstItem()
            {
                List.RemoveAt(0);
            }
        }
    }
}
