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
        SpellCheck.SetProvider(CustomProviderTextBox, new SampleSpellCheckProvider());
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

    // A custom provider only implements the three public interfaces.
    private sealed class SampleSpellCheckProvider : ISpellCheckProvider
    {
        public IReadOnlyList<CultureInfo> SupportedCultures { get; } =
            new[] { CultureInfo.InvariantCulture };

        // This sample checks one made up word, so it accepts any culture.
        public ISpellCheckContext CreateContext(CultureInfo culture) => new SampleContext();
    }

    private sealed class SampleContext : ISpellCheckContext
    {
        private const string Misspelling = "avlnia";
        private bool _disposed;

        public ValueTask<IReadOnlyList<ISpellCheckResult>> CheckAsync(
            ReadOnlyMemory<char> text,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();

            var index = text.Span.IndexOf(Misspelling, StringComparison.OrdinalIgnoreCase);

            return new ValueTask<IReadOnlyList<ISpellCheckResult>>(
                index < 0
                    ? Array.Empty<ISpellCheckResult>()
                    : new ISpellCheckResult[] { new SampleResult(this, index, Misspelling.Length) });
        }

        public ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            return new ValueTask<IReadOnlyList<string>>(new[] { "Avalonia" });
        }

        public void Dispose() => _disposed = true;
    }

    private sealed class SampleResult(SampleContext context, int start, int length) : ISpellCheckResult
    {
        public int Start => start;

        public int Length => length;

        public string? Description => null;

        public ValueTask<IReadOnlyList<string>> SuggestAsync(CancellationToken cancellationToken = default) =>
            context.SuggestAsync(cancellationToken);
    }
}
