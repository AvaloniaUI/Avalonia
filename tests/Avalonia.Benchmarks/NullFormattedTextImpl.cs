using System;
using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Platform;

namespace Avalonia.Benchmarks
{
    internal class NullFormattedTextImpl : IFormattedTextImpl
    {
        public Size Constraint { get; }

        public Rect Bounds { get; }

        public string Text { get; }

        public IEnumerable<FormattedTextLine> GetLines()
        {
            throw new NotImplementedException();
        }

        public TextHitTestResult HitTestPoint(Point point)
        {
            throw new NotImplementedException();
        }

        public Rect HitTestTextPosition(int index)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Rect> HitTestTextRange(int index, int length)
        {
            throw new NotImplementedException();
        }
    }
}
