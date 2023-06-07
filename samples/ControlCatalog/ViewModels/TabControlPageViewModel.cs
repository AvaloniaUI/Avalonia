using Avalonia.Controls;
using Avalonia.Media.Imaging;
using MiniMvvm;

namespace ControlCatalog.ViewModels;

public class TabControlPageViewModel : ViewModelBase
{
    private Dock _tabPlacement;

    public TabControlPageViewModelItem[]? Tabs { get; set; }

    public Dock TabPlacement
    {
        get { return _tabPlacement; }
        set { this.RaiseAndSetIfChanged(ref _tabPlacement, value); }
    }
}

public class TabControlPageViewModelItem
{
    public string? Header { get; set; }
    public string? Text { get; set; }
    public Bitmap? Image { get; set; }
    public bool IsEnabled { get; set; } = true;
}
