using Avalonia.Controls;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages;

public partial class TableViewPage : ContentPage
{
    public TableViewPage()
    {
        InitializeComponent();
        DataContext = new TableViewPageViewModel();
    }
}
