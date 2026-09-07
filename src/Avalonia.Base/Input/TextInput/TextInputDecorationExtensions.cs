using Avalonia.Media;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Helpers for building <see cref="TextInputDecoration"/>s.
    /// </summary>
    [Unstable]
    public static class TextInputDecorationExtensions
    {
        /// <summary>
        /// Builds a decoration for a composition clause addressed as an offset span relative to the
        /// composition start. Platform adapters receive clause attributes (IMM <c>GCS_COMPATTR</c>, Android
        /// composing spans) as offsets into the composition text; this resolves them to a document range
        /// against the composition range the adapter just set, so no composition-relative type is needed on
        /// the contract.
        /// </summary>
        /// <param name="navigation">The navigator that produced <paramref name="compositionStart"/>.</param>
        /// <param name="compositionStart">The composition start (<see cref="IStructuredTextInput.CompositionRange"/> Start).</param>
        /// <param name="clauseStart">The clause start, in UTF-16 code units relative to the composition start.</param>
        /// <param name="clauseLength">The clause length in UTF-16 code units.</param>
        /// <param name="kind">The semantic role of the clause.</param>
        /// <param name="foreground">Optional explicit foreground override.</param>
        /// <param name="background">Optional explicit background override.</param>
        /// <param name="underline">Optional explicit underline override.</param>
        public static TextInputDecoration CreateClauseDecoration(
            this ITextNavigation navigation,
            ITextPointer compositionStart,
            int clauseStart,
            int clauseLength,
            TextInputDecorationKind kind,
            Color? foreground = null,
            Color? background = null,
            TextInputUnderline underline = TextInputUnderline.None)
        {
            var start = navigation.GetPosition(compositionStart, clauseStart);
            var end = navigation.GetPosition(compositionStart, clauseStart + clauseLength);

            return new TextInputDecoration(navigation.GetRange(start, end), kind, foreground, background, underline);
        }
    }
}
