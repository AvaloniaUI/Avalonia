using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views
{
    internal class EventsPageView : UserControl
    {
        private readonly ListBox _events;

        public EventsPageView()
        {
            InitializeComponent();
            _events = this.FindControl<ListBox>("events");
        }

        private void RecordedEvents_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _events.ScrollIntoView(_events.Items.OfType<FiredEvent>().LastOrDefault());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
