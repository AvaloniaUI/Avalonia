using System;

namespace Avalonia.Input.TextInput
{
    public enum TextInputMethodPreeditSegmentKind
    {
        ActiveClause,
        InactiveClause
    }

    public readonly record struct TextInputMethodPreeditSegment(
        int Start,
        int Length,
        TextInputMethodPreeditSegmentKind Kind)
    {
        public int End => Start + Length;
    }
}
