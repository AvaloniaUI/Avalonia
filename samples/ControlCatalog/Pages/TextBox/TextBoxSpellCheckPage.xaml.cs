using System.Text;
using Avalonia.Controls;

namespace ControlCatalog.Pages;

public partial class TextBoxSpellCheckPage : UserControl
{
    public TextBoxSpellCheckPage()
    {
        InitializeComponent();
        LongSpellCheckTextBox.Text = CreateLongSpellCheckText();
    }

    private static string CreateLongSpellCheckText()
    {
        var builder = new StringBuilder();

        for (var i = 1; i <= 80; i++)
        {
            builder.Append("Line ");
            builder.Append(i);
            builder.Append(": Thiss long sample keeps severl intentional spelling erors visible while scrolling.");
            builder.AppendLine();
        }

        return builder.ToString();
    }
}
