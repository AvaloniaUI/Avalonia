using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Sandbox;

public class MainViewModel : INotifyPropertyChanged
{
    private string _selectedItem;
    public string SelectedItem
    {
        get { return _selectedItem; }
        set
        {
            _selectedItem = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(SelectedItem)));
            Property1 = value;
        }
    }

    private string _property1;
    public string Property1
    {
        get { return _property1; }
        set
        {
            _property1 = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Property1)));
        }
    }

    public ObservableCollection<string> Items { get; } = ["One", "Two", "Three"];

    public event PropertyChangedEventHandler? PropertyChanged;

    public void ButtonClick()
    {
        Property1 = $"From Button Click - {DateTime.Now: HH:mm:ss}";
    }
}
