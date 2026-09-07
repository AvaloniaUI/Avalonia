using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Composition delivery helpers for platform backends.
    /// </summary>
    /// <remarks>
    /// A client composes in its document exactly when it implements <see cref="IStructuredTextInput"/>.
    /// Such a client also reports <see cref="TextInputMethodClient.SupportsPreedit"/>, so a backend that
    /// gates composition on that flag keeps delivering to it; these helpers choose the route.
    /// </remarks>
    [Unstable]
    public static class TextInputMethodClientExtensions
    {
        /// <summary>
        /// Delivers composition text to the client: structured clients compose in the
        /// document through <see cref="IStructuredTextInput.SetCompositionText"/>, legacy
        /// overlay clients through
        /// <see cref="TextInputMethodClient.SetPreeditText(string?, int?)"/> when they
        /// declare <see cref="TextInputMethodClient.SupportsPreedit"/>. Null or empty
        /// text cancels the composition.
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
        /// Commits <paramref name="result"/> as the outcome of the composition. A structured client
        /// replaces its composition with the result in one change and the method returns true. Any
        /// other client has its preedit cleared and the method returns false: the caller then delivers
        /// the result as text input, as it would without a composition.
        /// </summary>
        public static bool CommitComposition(this TextInputMethodClient client, string? result)
        {
            if (client is IStructuredTextInput structured)
            {
                structured.CommitComposition(string.IsNullOrEmpty(result) ? null : result);
                return true;
            }

            if (client.SupportsPreedit)
            {
                client.SetPreeditText(null, null);
            }

            return false;
        }
    }
}
