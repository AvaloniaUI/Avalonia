using System;
using Avalonia.Media.TextFormatting;
using Avalonia.Metadata;

namespace Avalonia.Input.TextInput
{
    /// <summary>
    /// Provides structured text access and editing operations for IME integrations.
    /// </summary>
    /// <remarks>
    /// Offsets are UTF-16 code units and all members are UI-thread-only.
    /// </remarks>
    [Unstable]
    public interface IStructuredTextInput
    {
        ITextPointer DocumentStart { get; }
        ITextPointer DocumentEnd { get; }
        ITextRange DocumentRange { get; }

        string GetText(ITextRange range);
        ITextPointer CreatePointer(int offset, LogicalDirection direction);
        ITextRange CreateRange(ITextPointer start, ITextPointer end);

        ITextPointer CaretPosition { get; }
        ITextRange Selection { get; set; }
        ITextRange? CompositionRange { get; set; }

        void ReplaceText(ITextRange range, string text);
        void SetCompositionText(string? text, int cursorOffset);
        void CommitComposition();

        Rect GetFirstRectForRange(ITextRange range);
        Rect GetCaretRect(ITextPointer position);
        Rect[] GetSelectionRects(ITextRange range);
        ITextPointer? GetClosestPosition(Point point);
        ITextPointer? GetClosestPosition(Point point, ITextRange withinRange);
        ITextRange? GetCharacterRangeAtPoint(Point point);

        ITextRange? GetRangeEnclosing(ITextPointer position, TextGranularity granularity);
        ITextPointer? GetBoundaryPosition(ITextPointer position, TextGranularity granularity, LogicalDirection direction);

        event EventHandler? TextChanged;
        event EventHandler? CaretPositionChanged;
        event EventHandler? CompositionChanged;
    }
}
