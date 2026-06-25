using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input.TextInput;

namespace ControlCatalog.Pages;

public partial class TextBoxSpellCheckPage : UserControl
{
    public TextBoxSpellCheckPage()
    {
        InitializeComponent();
        TextInputOptions.SetSpellCheckProvider(CustomProviderTextBox, new SampleSpellCheckProvider());
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

    private sealed class SampleSpellCheckProvider : ISpellCheckProvider
    {
        public bool IsLanguageSupported(CultureInfo? culture) => true;

        public ValueTask<IReadOnlyList<SpellCheckResult>> CheckAsync(
            ReadOnlySpan<char> text,
            CultureInfo? culture,
            CancellationToken cancellationToken = default)
        {
            const string misspelling = "avlnia";
            var index = text.IndexOf(misspelling, StringComparison.OrdinalIgnoreCase);

            return new ValueTask<IReadOnlyList<SpellCheckResult>>(
                index < 0
                    ? Array.Empty<SpellCheckResult>()
                    : new[] { new SpellCheckResult(index, misspelling.Length, text.Slice(index, misspelling.Length).ToString()) });
        }

        public ValueTask<IReadOnlyList<string>> SuggestAsync(
            string word,
            CultureInfo? culture,
            CancellationToken cancellationToken = default)
        {
            return new ValueTask<IReadOnlyList<string>>(new[] { "Avalonia" });
        }
    }
}
