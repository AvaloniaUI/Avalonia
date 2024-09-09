using System;
using Avalonia.Input.TextInput;

namespace Avalonia.Android.Platform.Input
{
    internal abstract class EditCommand
    {
        public abstract void Apply(TextEditBuffer buffer);
    }

    internal class SelectionCommand : EditCommand
    {
        private readonly int _start;
        private readonly int _end;

        public SelectionCommand(int start, int end)
        {
            _start = Math.Min(start, end);
            _end = Math.Max(start, end);
        }

        public override void Apply(TextEditBuffer buffer)
        {
            var start = Math.Clamp(_start, 0, buffer.Text.Length);
            var end = Math.Clamp(_end, 0, buffer.Text.Length);
            buffer.Selection = new TextSelection(start, end);
        }
    }

    internal class CompositionRegionCommand : EditCommand
    {
        private readonly int _start;
        private readonly int _end;

        public CompositionRegionCommand(int start, int end)
        {
            _start = Math.Min(start, end);
            _end = Math.Max(start, end);
        }

        public override void Apply(TextEditBuffer buffer)
        {
            buffer.Composition = new TextSelection(_start, _end);
        }
    }

    internal class DeleteRegionCommand : EditCommand
    {
        private readonly int _before;
        private readonly int _after;

        public DeleteRegionCommand(int before, int after)
        {
            _before = before;
            _after = after;
        }

        public override void Apply(TextEditBuffer buffer)
        {
            var end = Math.Min(buffer.Text.Length, buffer.Selection.End + _after);
            var endCount = end - buffer.Selection.End;
            var start = Math.Max(0, buffer.Selection.Start - _before);
            buffer.Remove(buffer.Selection.End, endCount);
            buffer.Remove(start, buffer.Selection.Start - start);
            buffer.Selection = new TextSelection(start, start);
        }
    }

    internal class DeleteRegionInCodePointsCommand : EditCommand
    {
        private readonly int _before;
        private readonly int _after;

        public DeleteRegionInCodePointsCommand(int before, int after)
        {
            _before = before;
            _after = after;
        }

        public override void Apply(TextEditBuffer buffer)
        {
            var beforeLengthInChar = 0;

            for (int i = 0; i < _before; i++)
            {
                beforeLengthInChar++;
                if (buffer.Selection.Start > beforeLengthInChar)
                {
                    var lead = buffer.Text[buffer.Selection.Start - beforeLengthInChar - 1];
                    var trail = buffer.Text[buffer.Selection.Start - beforeLengthInChar];

                    if (char.IsSurrogatePair(lead, trail))
                    {
                        beforeLengthInChar++;
                    }
                }

                if (beforeLengthInChar == buffer.Selection.Start)
                    break;
            }

            var afterLengthInChar = 0;
            for (int i = 0; i < _after; i++)
            {
                afterLengthInChar++;
                if (buffer.Selection.End > afterLengthInChar)
                {
                    var lead = buffer.Text[buffer.Selection.End + afterLengthInChar - 1];
                    var trail = buffer.Text[buffer.Selection.End + afterLengthInChar];

                    if (char.IsSurrogatePair(lead, trail))
                    {
                        afterLengthInChar++;
                    }
                }

                if (buffer.Selection.End + afterLengthInChar == buffer.Text.Length)
                    break;
            }

            var start = buffer.Selection.Start - beforeLengthInChar;
            buffer.Remove(buffer.Selection.End, afterLengthInChar);
            buffer.Remove(start, beforeLengthInChar);
            buffer.Selection = new TextSelection(start, start);
        }
    }

    internal class CompositionTextCommand : EditCommand
    {
        private readonly string _text;
        private readonly int _newCursorPosition;

        public CompositionTextCommand(string text, int newCursorPosition)
        {
            _text = text;
            _newCursorPosition = newCursorPosition;
        }

        public override void Apply(TextEditBuffer buffer)
        {
            buffer.ComposingText = _text;
            var newCursor = _newCursorPosition > 0 ? buffer.Selection.Start + _newCursorPosition - 1 : buffer.Selection.Start + _newCursorPosition;
            buffer.Selection = new TextSelection(newCursor, newCursor);
        }
    }

    internal class CommitTextCommand : EditCommand
    {
        private readonly string _text;
        private readonly int _newCursorPosition;

        public CommitTextCommand(string text, int newCursorPosition)
        {
            _text = text;
            _newCursorPosition = newCursorPosition;
        }

        public override void Apply(TextEditBuffer buffer)
        {
            if (buffer.HasComposition)
            {
                buffer.Replace(buffer.Composition!.Value.Start, buffer.Composition!.Value.End, _text);
            }
            else
            {
                buffer.Replace(buffer.Selection.Start, buffer.Selection.End, _text);
            }
            var newCursor = _newCursorPosition > 0 ? buffer.Selection.Start + _newCursorPosition - 1 : buffer.Selection.Start + _newCursorPosition - _text.Length;
            buffer.Selection = new TextSelection(newCursor, newCursor);
        }
    }

    internal class FinishComposingCommand : EditCommand
    {
        public override void Apply(TextEditBuffer buffer)
        {
            buffer.Composition = default;
        }
    }
}
