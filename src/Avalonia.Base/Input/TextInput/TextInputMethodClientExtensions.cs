using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Composition delivery helpers for platform backends.
    /// </summary>
    [Unstable]
    public static class TextInputMethodClientExtensions
    {
        /// <summary>
        /// Delivers composition text to the client: structured clients compose in the
        /// document through <see cref="IStructuredTextInput.SetCompositionText"/>, legacy
        /// overlay clients through
        /// <see cref="TextInputMethodClient.SetPreeditText(string?, int?)"/> when they
        /// declare <see cref="TextInputMethodClient.SupportsPreedit"/>. Null or empty
        /// text ends the composition.
        /// </summary>
        public static void DeliverComposition(this TextInputMethodClient client, string? text, int? cursor)
        {
            if (client is IStructuredTextInput structured)
            {
                if (string.IsNullOrEmpty(text))
                {
                    structured.SetCompositionText(null, 0);
                }
                else
                {
                    structured.SetCompositionText(text, cursor ?? text.Length);
                }

                return;
            }

            if (client.SupportsPreedit)
            {
                client.SetPreeditText(text, cursor);
            }
        }

        /// <summary>
        /// Whether the client displays uncommitted composition inline - in the document
        /// for structured clients, through the preedit overlay for legacy ones.
        /// </summary>
        public static bool SupportsInlineComposition(this TextInputMethodClient client)
            => client is IStructuredTextInput || client.SupportsPreedit;
    }
}
