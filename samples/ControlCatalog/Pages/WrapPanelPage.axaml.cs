using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages;

public partial class WrapPanelPage : ContentPage
{
    public WrapPanelPage()
    {
        InitializeComponent();
        DataContext = new WrapPanelPageViewModel();
    }
}
