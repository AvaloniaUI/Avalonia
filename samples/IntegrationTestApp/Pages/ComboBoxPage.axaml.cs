using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp.Pages;

public partial class ComboBoxPage : UserControl
{
    public ComboBoxPage()
    {
        InitializeComponent();
    }

    private void ComboBoxSelectionClear_Click(object? sender, RoutedEventArgs e)
    {
        BasicComboBox.SelectedIndex = -1;
    }

    private void ComboBoxSelectFirst_Click(object? sender, RoutedEventArgs e)
    {
        BasicComboBox.SelectedIndex = 0;
    }
}
