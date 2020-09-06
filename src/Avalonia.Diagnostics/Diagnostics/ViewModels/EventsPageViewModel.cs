using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class EventsPageViewModel : ViewModelBase
    {
        private readonly IControl _root;
        private FiredEvent _selectedEvent;

        public EventsPageViewModel(IControl root)
        {
            _root = root;

            Nodes = RoutedEventRegistry.Instance.GetAllRegistered()
                .GroupBy(e => e.OwnerType)
                .OrderBy(e => e.Key.Name)
                .Select(g => new EventOwnerTreeNode(g.Key, g, this))
                .ToArray();
        }

        public string Name => "Events";

        public EventTreeNodeBase[] Nodes { get; }

        public ObservableCollection<FiredEvent> RecordedEvents { get; } = new ObservableCollection<FiredEvent>();

        public FiredEvent SelectedEvent
        {
            get => _selectedEvent;
            set => RaiseAndSetIfChanged(ref _selectedEvent, value);
        }

        private void Clear()
        {
            RecordedEvents.Clear();
        }
    }
}
