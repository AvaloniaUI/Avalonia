using MiniMvvm;

namespace VirtualizationDemo.ViewModels;

public class ExpanderItemViewModel : ViewModelBase
{
    private string? _header;
    private bool _isExpanded;

    public string? Header 
    { 
        get => _header;
        set => RaiseAndSetIfChanged(ref _header, value);
    }

    public bool IsExpanded
    {
        get => _isExpanded;
        set => RaiseAndSetIfChanged(ref _isExpanded, value);
    }
}
