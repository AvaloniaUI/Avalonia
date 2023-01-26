using Avalonia.Controls;
using Avalonia.Media.Imaging;
using MiniMvvm;

namespace ControlCatalog.ViewModels;

public class PivotPageViewModel : ViewModelBase
{
    private PivotHeaderPlacement _tabPlacement;

    public TabControlPageViewModelItem[]? Tabs { get; set; }

    public PivotHeaderPlacement TabPlacement
    {
        get { return _tabPlacement; }
        set { this.RaiseAndSetIfChanged(ref _tabPlacement, value); }
    }
}
