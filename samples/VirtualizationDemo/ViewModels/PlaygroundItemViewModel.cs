using MiniMvvm;

namespace VirtualizationDemo.ViewModels;

public class PlaygroundItemViewModel : ViewModelBase
{
    private string? _header;

    public PlaygroundItemViewModel(int index) => Header = $"Item {index}";
    public PlaygroundItemViewModel(string? header) => Header = header;

    public string? Header
    {
        get => _header;
        set => RaiseAndSetIfChanged(ref _header, value);
    }
}
