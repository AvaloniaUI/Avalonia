using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;
using Avalonia.Input.TextInput;
using Avalonia.Media.TextFormatting;

namespace Avalonia.FreeDesktop.AtSpi
{
    /// <summary>
    /// The AT-SPI view of a control's accessible text: one string of characters, counted in Unicode
    /// code points as AT-SPI counts them, mapped to and from the control's positions.
    /// </summary>
    /// <remarks>
    /// The string is the plain text of each linear scope in document order, with a line feed between
    /// two scopes. AT-SPI offsets live only here; the navigation never sees them. Inside a scope the
    /// plain text lines up with the scope's code units, so a code point index converts to a scope offset
    /// without walking the document. The view is rebuilt lazily after the text or the scope structure
    /// changes.
    /// </remarks>
    internal sealed class AtSpiTextModel
    {
        private const char ScopeSeparator = '\n';

        private readonly IAccessibleText _text;
        private readonly List<Segment> _segments = new();
        private readonly List<ILinearTextScope> _subscribedScopes = new();
        private string _plain = string.Empty;
        private int _length;
        private bool _stale = true;
        private string? _reported;

        public AtSpiTextModel(IAccessibleText text)
        {
            _text = text;
        }

        public IAccessibleText Text => _text;

        /// <summary>The number of characters (code points).</summary>
        public int Length
        {
            get
            {
                EnsureCurrent();
                return _length;
            }
        }

        /// <summary>The whole text as a string, sliced by the <see cref="char"/> indices <see cref="ToUtf16"/> returns.</summary>
        public string PlainText
        {
            get
            {
                EnsureCurrent();
                return _plain;
            }
        }

        /// <summary>Marks the view out of date; the next query rebuilds it.</summary>
        public void Invalidate() => _stale = true;

        /// <summary>
        /// Rebuilds the view after a text change and returns what changed since the previous call, in
        /// characters, or null when the text reads the same. The first call only records a baseline.
        /// </summary>
        public (int Offset, string Removed, string Inserted)? TakeChange()
        {
            Invalidate();
            var current = PlainText;
            var previous = _reported;
            _reported = current;

            if (previous is null || string.Equals(previous, current, StringComparison.Ordinal))
            {
                return null;
            }

            var prefix = 0;
            var maxPrefix = Math.Min(previous.Length, current.Length);
            while (prefix < maxPrefix && previous[prefix] == current[prefix])
            {
                prefix++;
            }

            var suffix = 0;
            var maxSuffix = Math.Min(previous.Length, current.Length) - prefix;
            while (suffix < maxSuffix && previous[previous.Length - 1 - suffix] == current[current.Length - 1 - suffix])
            {
                suffix++;
            }

            // Never cut a surrogate pair: widen the changed span to whole code points on both sides.
            if (prefix > 0 && char.IsLowSurrogate(At(current, prefix)) && char.IsHighSurrogate(current[prefix - 1]))
            {
                prefix--;
            }

            if (suffix > 0 && char.IsLowSurrogate(At(current, current.Length - suffix)) &&
                char.IsHighSurrogate(At(current, current.Length - suffix - 1)))
            {
                suffix--;
            }

            var removed = previous.Substring(prefix, previous.Length - suffix - prefix);
            var inserted = current.Substring(prefix, current.Length - suffix - prefix);
            return (CodePoints(current, 0, prefix), removed, inserted);
        }

        /// <summary>Starts the change baseline at the current text without reporting anything.</summary>
        public void RecordBaseline()
        {
            Invalidate();
            _reported = PlainText;
        }

        /// <summary>The character offset of <paramref name="position"/>.</summary>
        public int ToOffset(ITextPointer position)
        {
            EnsureCurrent();

            var scope = _text.GetLinearScope(position);
            foreach (var segment in _segments)
            {
                if (segment.Scope.Range.Start.Equals(scope.Range.Start))
                {
                    var utf16 = Math.Clamp(scope.GetOffset(position), 0, segment.Utf16Length);
                    return segment.Start + CodePoints(_plain, segment.Utf16Start, utf16);
                }
            }

            return 0;
        }

        /// <summary>The position at character <paramref name="offset"/>, clamped to the text.</summary>
        public ITextPointer ToPosition(int offset, LogicalDirection gravity = LogicalDirection.Forward)
        {
            EnsureCurrent();

            if (_segments.Count == 0)
            {
                return _text.WithGravity(_text.DocumentStart, gravity);
            }

            offset = Math.Clamp(offset, 0, _length);
            foreach (var segment in _segments)
            {
                if (offset <= segment.Start + segment.Length)
                {
                    var units = ToUtf16Within(segment, Math.Max(0, offset - segment.Start));
                    return segment.Scope.GetPosition(units, gravity);
                }
            }

            var last = _segments[_segments.Count - 1];
            return last.Scope.GetPosition(last.Utf16Length, gravity);
        }

        /// <summary>The range between two character offsets, normalized and clamped.</summary>
        public ITextRange ToRange(int start, int end)
        {
            if (end < start)
            {
                (start, end) = (end, start);
            }

            return _text.GetRange(ToPosition(start, LogicalDirection.Forward), ToPosition(end, LogicalDirection.Backward));
        }

        /// <summary>The <see cref="char"/> index into <see cref="PlainText"/> of character <paramref name="offset"/>.</summary>
        public int ToUtf16(int offset)
        {
            EnsureCurrent();
            offset = Math.Clamp(offset, 0, _length);

            var index = 0;
            for (var count = 0; count < offset && index < _plain.Length; count++)
            {
                index += IsPairAt(_plain, index) ? 2 : 1;
            }

            return index;
        }

        /// <summary>The characters between two offsets, clamped to the text.</summary>
        public string Substring(int start, int end)
        {
            start = Math.Clamp(start, 0, Length);
            end = Math.Clamp(end, start, Length);
            var from = ToUtf16(start);
            return _plain.Substring(from, ToUtf16(end) - from);
        }

        /// <summary>The code point at <paramref name="offset"/>, or -1 outside the text.</summary>
        public int CharacterAt(int offset)
        {
            if (offset < 0 || offset >= Length)
            {
                return -1;
            }

            return char.ConvertToUtf32(_plain, ToUtf16(offset));
        }

        /// <summary>The number of code points in <paramref name="text"/>.</summary>
        public static int CodePoints(string text) => CodePoints(text, 0, text.Length);

        private void EnsureCurrent()
        {
            if (!_stale)
            {
                return;
            }

            _stale = false;
            UnsubscribeScopes();
            _segments.Clear();

            var builder = new System.Text.StringBuilder();
            var length = 0;
            var position = _text.DocumentStart;
            ITextPointer? previousEnd = null;

            while (true)
            {
                var scope = _text.GetLinearScope(position);
                if (previousEnd is not null && scope.Range.End.CompareTo(previousEnd) <= 0)
                {
                    break;
                }

                if (previousEnd is not null)
                {
                    builder.Append(ScopeSeparator);
                    length++;
                }

                var plain = _text.GetPlainText(scope.Range);
                var codePoints = CodePoints(plain);
                _segments.Add(new Segment(scope, length, codePoints, builder.Length, plain.Length));
                builder.Append(plain);
                length += codePoints;

                scope.Invalidated += OnScopeInvalidated;
                _subscribedScopes.Add(scope);

                var end = scope.Range.End;
                var next = _text.GetPosition(end, TextUnit.Character, 1);
                if (next.CompareTo(end) <= 0)
                {
                    break;
                }

                previousEnd = end;
                position = next;
            }

            _plain = builder.ToString();
            _length = length;
        }

        private void OnScopeInvalidated(object? sender, EventArgs e) => _stale = true;

        private void UnsubscribeScopes()
        {
            foreach (var scope in _subscribedScopes)
            {
                scope.Invalidated -= OnScopeInvalidated;
            }

            _subscribedScopes.Clear();
        }

        /// <summary>Stops listening to the scopes; the model must not be used afterwards.</summary>
        public void Detach() => UnsubscribeScopes();

        private int ToUtf16Within(Segment segment, int codePoints)
        {
            var index = segment.Utf16Start;
            var end = segment.Utf16Start + segment.Utf16Length;
            for (var count = 0; count < codePoints && index < end; count++)
            {
                index += IsPairAt(_plain, index) ? 2 : 1;
            }

            return index - segment.Utf16Start;
        }

        private static int CodePoints(string text, int start, int length)
        {
            var count = 0;
            var end = Math.Min(text.Length, start + length);
            for (var i = start; i < end; i++)
            {
                count++;
                if (IsPairAt(text, i))
                {
                    i++;
                }
            }

            return count;
        }

        private static bool IsPairAt(string text, int index)
            => index + 1 < text.Length && char.IsHighSurrogate(text[index]) && char.IsLowSurrogate(text[index + 1]);

        private static char At(string text, int index) => index >= 0 && index < text.Length ? text[index] : '\0';

        private readonly record struct Segment(
            ILinearTextScope Scope, int Start, int Length, int Utf16Start, int Utf16Length);
    }
}
