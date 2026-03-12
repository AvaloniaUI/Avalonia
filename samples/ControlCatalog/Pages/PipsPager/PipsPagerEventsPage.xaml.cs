using System.Collections.ObjectModel;
using Avalonia.Controls;

namespace ControlCatalog.Pages;

public partial class PipsPagerEventsPage : UserControl
{
    private readonly ObservableCollection<string> _events = new();

    public PipsPagerEventsPage()
    {
        InitializeComponent();

        EventLog.ItemsSource = _events;

        EventPager.PropertyChanged += (_, e) =>
        {
            if (e.Property != PipsPager.SelectedPageIndexProperty)
                return;

            var newIndex = (int)e.NewValue!;
            StatusText.Text = $"Selected: {newIndex}";
            _events.Insert(0, $"SelectedPageIndex changed to {newIndex}");

            if (_events.Count > 20)
                _events.RemoveAt(_events.Count - 1);
        };
    }
}
