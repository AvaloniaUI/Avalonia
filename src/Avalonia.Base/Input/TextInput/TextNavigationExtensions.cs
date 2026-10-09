using Avalonia.Media.TextFormatting;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Offset-addressing conveniences over <see cref="ITextNavigation"/> for platform adapters that
    /// speak integer offsets. All resolution goes through the navigator's produced anchors, so the
    /// provenance rule (no pointer fabricated from a bare integer) is preserved.
    /// </summary>
    internal static class TextNavigationExtensions
    {
        /// <summary>Resolves a document offset to a position with forward gravity.</summary>
        public static ITextPointer PointerAt(this ITextNavigation navigation, int offset)
            => navigation.GetPosition(navigation.DocumentStart, offset);

        /// <summary>
        /// Resolves a document offset to a position carrying the requested gravity. Forward gravity
        /// resolves from <see cref="ITextNavigation.DocumentStart"/>, backward from
        /// <see cref="ITextNavigation.DocumentEnd"/>, so the travel direction - which the navigator
        /// reports back as the result's gravity and uses to snap mid-unit offsets - matches the request.
        /// </summary>
        public static ITextPointer PointerAt(this ITextNavigation navigation, int offset, LogicalDirection gravity)
            => gravity == LogicalDirection.Forward
                ? navigation.GetPosition(navigation.DocumentStart, offset)
                : navigation.GetPosition(navigation.DocumentEnd, offset - navigation.DocumentEnd.Offset);

        /// <summary>
        /// A normalized range over the offset span [<paramref name="start"/>, start + length), with
        /// inward-facing endpoint gravity (forward at the start, backward at the end).
        /// </summary>
        public static ITextRange RangeAt(this ITextNavigation navigation, int start, int length)
            => navigation.GetRange(
                navigation.PointerAt(start, LogicalDirection.Forward),
                navigation.PointerAt(start + length, LogicalDirection.Backward));
    }
}
