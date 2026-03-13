using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Specifies text boundary granularity for tokenizer queries.
    /// </summary>
    [Unstable]
    public enum TextGranularity
    {
        Character,
        Word,
        Sentence,
        Line,
        Paragraph,
        Document
    }
}
