using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Sandbox;

public partial class ViewModel : ObservableObject
{
    [ObservableProperty] private int _selected;
    
    [JsonIgnore] public ObservableCollection<ComboBoxViewModel> DocumentOpenItems { get; } = new();

    public ViewModel()
    {
        DocumentOpenItems.Add(new ComboBoxViewModel
        {
            Text = "None",
            Value = 0
        });
        DocumentOpenItems.Add(new ComboBoxViewModel
        {
            Text = "Last session", 
            Value = 1
        });
    }
}

public partial class ComboBoxViewModel : ObservableObject
{
    [ObservableProperty] private string? _text;
    [ObservableProperty] private object? _value;

    public override string ToString() => Text ?? GetType().ToString();
}