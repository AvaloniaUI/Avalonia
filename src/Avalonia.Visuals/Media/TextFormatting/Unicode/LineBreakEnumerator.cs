// RichTextKit
// Copyright © 2019 Topten Software. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License"); you may 
// not use this product except in compliance with the License. You may obtain 
// a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, WITHOUT 
// WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. See the 
// License for the specific language governing permissions and limitations 
// under the License.
//
// Ported from: https://github.com/foliojs/linebreak
// Copied from: https://github.com/toptensoftware/RichTextKit

using Avalonia.Utilities;

namespace Avalonia.Media.TextFormatting.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Line Break Algorithm
    /// </summary>
    public ref struct LineBreakEnumerator
    {
        // State
        private readonly ReadOnlySlice<char> _text;
        private int _pos;
        private int _lastPos;
        private LineBreakClass? _curClass;
        private LineBreakClass? _nextClass;

        public LineBreakEnumerator(ReadOnlySlice<char> text)
        {
            _text = text;
            _pos = 0;
            _lastPos = 0;
            _curClass = null;
            _nextClass = null;
            Current = default;
        }

        public LineBreak Current { get; private set; }

        public bool MoveNext()
        {
            // get the first char if we're at the beginning of the string
            if (!_curClass.HasValue)
            {
                _curClass = PeekCharClass() == LineBreakClass.Space ? LineBreakClass.WordJoiner : MapFirst(ReadCharClass());
            }

            while (_pos < _text.Length)
            {
                _lastPos = _pos;
                var lastClass = _nextClass;
                _nextClass = ReadCharClass();

                // explicit newline
                if (_curClass.HasValue && (_curClass == LineBreakClass.MandatoryBreak || _curClass == LineBreakClass.CarriageReturn && _nextClass != LineBreakClass.LineFeed))
                {
                    _curClass = MapFirst(MapClass(_nextClass.Value));
                    Current = new LineBreak(FindPriorNonWhitespace(_lastPos), _lastPos, true);
                    return true;
                }

                // handle classes not handled by the pair table
                LineBreakClass? cur = null;
                switch (_nextClass.Value)
                {
                    case LineBreakClass.Space:
                        cur = _curClass;
                        break;

                    case LineBreakClass.MandatoryBreak:
                    case LineBreakClass.LineFeed:
                    case LineBreakClass.NextLine:
                        cur = LineBreakClass.MandatoryBreak;
                        break;

                    case LineBreakClass.CarriageReturn:
                        cur = LineBreakClass.CarriageReturn;
                        break;

                    case LineBreakClass.ContingentBreak:
                        cur = LineBreakClass.BreakAfter;
                        break;
                }

                if (cur != null)
                {
                    _curClass = cur;

                    if (_nextClass.Value == LineBreakClass.MandatoryBreak)
                    {
                        _lastPos = _pos;
                        Current = new LineBreak(FindPriorNonWhitespace(_lastPos), _lastPos, true);
                        return true;
                    }

                    continue;
                }

                // if not handled already, use the pair table
                var shouldBreak = false;
                switch (BreakPairTable.Map(_curClass.Value,_nextClass.Value))
                {
                    case PairBreakType.DI: // Direct break
                        shouldBreak = true;
                        break;

                    case PairBreakType.IN: // possible indirect break
                        shouldBreak = lastClass.HasValue && lastClass.Value == LineBreakClass.Space;
                        break;

                    case PairBreakType.CI:
                        shouldBreak = lastClass.HasValue && lastClass.Value == LineBreakClass.Space;
                        if (!shouldBreak)
                        {
                            continue;
                        }
                        break;

                    case PairBreakType.CP: // prohibited for combining marks
                        if (!lastClass.HasValue || lastClass.Value != LineBreakClass.Space)
                        {
                            continue;
                        }
                        break;
                }

                _curClass = _nextClass;

                if (shouldBreak)
                {
                    Current = new LineBreak(FindPriorNonWhitespace(_lastPos), _lastPos);
                    return true;
                }
            }

            if (_pos >= _text.Length)
            {
                if (_lastPos < _text.Length)
                {
                    _lastPos = _text.Length;
                    var cls = Codepoint.ReadAt(_text, _text.Length - 1, out _).LineBreakClass;
                    bool required = cls == LineBreakClass.MandatoryBreak || cls == LineBreakClass.LineFeed || cls == LineBreakClass.CarriageReturn;
                    Current = new LineBreak(FindPriorNonWhitespace(_text.Length), _text.Length, required);
                    return true;
                }
            }

            return false;
        }

        private int FindPriorNonWhitespace(int from)
        {
            if (from > 0)
            {
                var cp = Codepoint.ReadAt(_text, from - 1, out var count);

                var cls = cp.LineBreakClass;

                if (cls == LineBreakClass.MandatoryBreak || cls == LineBreakClass.LineFeed || cls == LineBreakClass.CarriageReturn)
                {
                    from -= count;
                }
            }

            while (from > 0)
            {
                var cp = Codepoint.ReadAt(_text, from - 1, out var count);

                var cls = cp.LineBreakClass;

                if (cls == LineBreakClass.Space)
                {
                    from -= count;
                }
                else
                {
                    break;
                }
            }
            return from;
        }

        // Get the next character class
        private LineBreakClass ReadCharClass()
        {
            var cp = Codepoint.ReadAt(_text, _pos, out var count);

            _pos += count;

            return MapClass(cp.LineBreakClass);
        }

        private LineBreakClass PeekCharClass()
        {
            return MapClass(Codepoint.ReadAt(_text, _pos, out _).LineBreakClass);
        }

        private static LineBreakClass MapClass(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.Ambiguous:
                    return LineBreakClass.Alphabetic;

                case LineBreakClass.ComplexContext:
                case LineBreakClass.Surrogate:
                case LineBreakClass.Unknown:
                    return LineBreakClass.Alphabetic;

                case LineBreakClass.ConditionalJapaneseStarter:
                    return LineBreakClass.Nonstarter;

                default:
                    return c;
            }
        }

        private static LineBreakClass MapFirst(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.LineFeed:
                case LineBreakClass.NextLine:
                    return LineBreakClass.MandatoryBreak;

                case LineBreakClass.ContingentBreak:
                    return LineBreakClass.BreakAfter;

                case LineBreakClass.Space:
                    return LineBreakClass.WordJoiner;

                default:
                    return c;
            }
        }
    }
}
