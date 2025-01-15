using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class ListBoxPage : UserControl
{
    public ListBoxPage()
    {
        InitializeComponent();
        ListBoxItems = Enumerable.Range(0, 100).Select(x => "Item " + x).ToList();
        DataContext = this;
    }

    public List<string> ListBoxItems { get; }

    private void ListBoxSelectionClear_Click(object? sender, RoutedEventArgs e)
    {
        BasicListBox.SelectedIndex = -1;
    }
}
