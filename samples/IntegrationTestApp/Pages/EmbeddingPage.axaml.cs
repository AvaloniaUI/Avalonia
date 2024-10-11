using Avalonia.Controls;
using Avalonia.Interactivity;

namespace IntegrationTestApp;

public partial class EmbeddingPage : UserControl
{
    public EmbeddingPage()
    {
        InitializeComponent();
        ResetText();
    }

    private void ResetText()
    {
        NativeTextBox.Text = NativeTextBoxInPopup.Text = "Native text box";
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        ResetText();
    }
}
