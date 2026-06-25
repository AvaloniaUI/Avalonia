using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Input.TextInput;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;

namespace Avalonia.Controls
{
    /// <summary>
    /// An <see cref="ITextNavigation"/> over a <see cref="TextBox"/>'s text, used by the automation
    /// peer (and the accessibility text providers). It shares the segmentation algorithms with the IME
    /// navigation via <see cref="TextSegmentation"/>; the IME client keeps its own copy until a later
    /// pass routes both through one navigation instance.
    /// </summary>
    internal sealed class TextBoxTextNavigation : IAccessibleText
    {
        private readonly TextBox _textBox;
        private long _version;
        private EventHandler<TextChange>? _textChanged;

        public TextBoxTextNavigation(TextBox textBox)
        {
            _textBox = textBox ?? throw new ArgumentNullException(nameof(textBox));
            _textBox.PropertyChanged += OnTextBoxPropertyChanged;
        }

        public ITextPointer DocumentStart => CreatePointer(0);

        public ITextPointer DocumentEnd => CreatePointer(Text.Length);

        public ITextRange DocumentRange => new NavRange(CreatePointer(0), CreatePointer(Text.Length));

        public long DocumentVersion => _version;

        public event EventHandler<TextChange>? TextChanged
        {
            add => _textChanged += value;
            remove => _textChanged -= value;
        }

        private string Text => _textBox.Text ?? string.Empty;

        public ITextPointer GetPosition(ITextPointer origin, int distance)
        {
            var text = Text;
            var target = Math.Clamp(OffsetOf(origin) + distance, 0, text.Length);

            return CreatePointer(TextSegmentation.SnapToValid(target, text, distance >= 0));
        }

        public ITextPointer GetPosition(ITextPointer origin, TextUnit unit, int count)
        {
            var offset = OffsetOf(origin);

            if (count == 0)
            {
                return CreatePointer(offset);
            }

            var text = Text;
            var forward = count > 0;
            var current = offset;

            for (var i = 0; i < Math.Abs(count); i++)
            {
                var next = TextSegmentation.MoveByUnit(current, unit, forward, text);
                if (next == current)
                {
                    break;
                }

                current = next;
            }

            return CreatePointer(current);
        }

        public ITextRange GetRangeEnclosing(ITextPointer position, TextUnit unit)
        {
            var text = Text;
            var offset = OffsetOf(position);

            switch (unit)
            {
                case TextUnit.Document:
                case TextUnit.Page:
                case TextUnit.Format:
                    return new NavRange(CreatePointer(0), CreatePointer(text.Length));

                case TextUnit.Character:
                    if (text.Length == 0)
                    {
                        return new NavRange(CreatePointer(0), CreatePointer(0));
                    }

                    var characterStart = Math.Clamp(offset, 0, text.Length - 1);
                    return new NavRange(CreatePointer(characterStart), CreatePointer(TextSegmentation.NextGrapheme(characterStart, text)));

                case TextUnit.Word:
                    var (wordStart, wordEnd) = TextSegmentation.WordBounds(offset, text);
                    return new NavRange(CreatePointer(wordStart), CreatePointer(wordEnd));

                case TextUnit.Sentence:
                    var (sentenceStart, sentenceEnd) = TextSegmentation.SentenceBounds(offset, text);
                    return new NavRange(CreatePointer(sentenceStart), CreatePointer(sentenceEnd));

                default:
                    var (lineStart, lineEnd) = TextSegmentation.LineBounds(offset, text);
                    return new NavRange(CreatePointer(lineStart), CreatePointer(lineEnd));
            }
        }

        public ITextRange GetRange(ITextPointer a, ITextPointer b)
        {
            var oa = OffsetOf(a);
            var ob = OffsetOf(b);

            return oa <= ob
                ? new NavRange(CreatePointer(oa), CreatePointer(ob))
                : new NavRange(CreatePointer(ob), CreatePointer(oa));
        }

        public int GetOffset(ITextPointer from, ITextPointer to) => OffsetOf(to) - OffsetOf(from);

        public string GetText(ITextRange range)
        {
            var text = Text;
            var start = OffsetOf(range.Start);
            var end = OffsetOf(range.End);
            if (start > end)
            {
                (start, end) = (end, start);
            }

            return text.Substring(start, end - start);
        }

        public void SetSelection(ITextRange range)
        {
            _textBox.SelectionStart = OffsetOf(range.Start);
            _textBox.SelectionEnd = OffsetOf(range.End);
        }

        public ITextRange GetSelection()
        {
            var start = Math.Min(_textBox.SelectionStart, _textBox.SelectionEnd);
            var end = Math.Max(_textBox.SelectionStart, _textBox.SelectionEnd);
            return GetRange(CreatePointer(start), CreatePointer(end));
        }

        public Rect[] GetBoundingRectangles(ITextRange range)
        {
            var start = OffsetOf(range.Start);
            var end = OffsetOf(range.End);
            if (start > end)
            {
                (start, end) = (end, start);
            }

            return _textBox.GetTextRangeBounds(start, end - start);
        }

        public (IReadOnlyDictionary<TextAttribute, object?> Attributes, ITextRange Run) GetTextAttributes(ITextPointer position)
        {
            // Validate the pointer is ours; a TextBox is uniform, so the offset itself is unused.
            _ = OffsetOf(position);

            var attributes = new Dictionary<TextAttribute, object?>
            {
                [TextAttribute.FontFamily] = _textBox.FontFamily?.Name,
                [TextAttribute.FontSize] = _textBox.FontSize,
                [TextAttribute.FontWeight] = _textBox.FontWeight,
                [TextAttribute.FontStyle] = _textBox.FontStyle,
                [TextAttribute.IsReadOnly] = _textBox.IsReadOnly,
            };

            if ((_textBox.Foreground as ISolidColorBrush)?.Color is { } foreground)
            {
                attributes[TextAttribute.Foreground] = foreground;
            }

            if ((_textBox.Background as ISolidColorBrush)?.Color is { } background)
            {
                attributes[TextAttribute.Background] = background;
            }

            return (attributes, DocumentRange);
        }

        private void OnTextBoxPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != TextBox.TextProperty)
            {
                return;
            }

            var (offset, oldLength, newLength) =
                TextSegmentation.ComputeChange(e.OldValue as string ?? string.Empty, e.NewValue as string ?? string.Empty);

            if (oldLength == 0 && newLength == 0)
            {
                return;
            }

            _version++;
            _textChanged?.Invoke(this, new TextChange(CreatePointer(offset), oldLength, newLength));
        }

        private ITextPointer CreatePointer(int offset) =>
            new NavPointer(this, Math.Clamp(offset, 0, Text.Length), LogicalDirection.Forward);

        private int OffsetOf(ITextPointer pointer)
        {
            if (pointer is not NavPointer own || own.Owner != this)
            {
                throw new ArgumentException("The text pointer was not produced by this navigator.", nameof(pointer));
            }

            return Math.Clamp(own.Offset, 0, Text.Length);
        }

        private sealed class NavPointer : ITextPointer
        {
            public NavPointer(TextBoxTextNavigation owner, int offset, LogicalDirection direction)
            {
                Owner = owner;
                Offset = offset;
                Gravity = direction;
            }

            public TextBoxTextNavigation Owner { get; }

            public int Offset { get; }

            public LogicalDirection Gravity { get; }

            public int CompareTo(ITextPointer? other) => other is null ? 1 : Offset.CompareTo(other.Offset);

            public bool Equals(ITextPointer? other) => other is not null && Offset == other.Offset;

            public override bool Equals(object? obj) => obj is ITextPointer other && Equals(other);

            public override int GetHashCode() => Offset;
        }

        private sealed class NavRange : ITextRange
        {
            public NavRange(ITextPointer start, ITextPointer end)
            {
                Start = start;
                End = end;
            }

            public ITextPointer Start { get; }

            public ITextPointer End { get; }

            public bool IsEmpty => Start.Offset == End.Offset;
        }
    }
}
