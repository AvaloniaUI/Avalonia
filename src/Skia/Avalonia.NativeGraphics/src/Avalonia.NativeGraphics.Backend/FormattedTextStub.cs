using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.NativeGraphics.Backend
{
    internal class FormattedTextStub : IFormattedTextImpl
    {
        public FormattedTextStub(string text)
        {
            Text = text;
        }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            yield break;
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            return new TextHitTestResult();
        }

        public Rect HitTestTextPosition(int index)
        {
            return default;
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            yield break;
        }

        public Size Constraint => new Size(10, 10);
        public Rect Bounds => new Rect(0, 0, 10, 10);
        public string Text { get; }
    }
}