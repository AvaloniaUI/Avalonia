using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views
{
    public class EventsView : UserControl
    {
        ListBox _events;

        public EventsView()
        {
            this.InitializeComponent();
            _events = this.FindControl<ListBox>("events");
        }

        private void RecordedEvents_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _events.ScrollIntoView(_events.Items.OfType<FiredEvent>().LastOrDefault());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
