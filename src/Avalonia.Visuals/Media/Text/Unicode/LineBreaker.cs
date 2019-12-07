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

using Avalonia.Utility;

namespace Avalonia.Media.Text.Unicode
{
    /// <summary>
    /// Implementation of the Unicode Line Break Algorithm
    /// </summary>
    internal ref struct LineBreaker
    {
        // State
        private readonly ReadOnlySlice<char> _codePoints;
        private int _pos;
        private int _lastPos;
        private LineBreakClass? _curClass;
        private LineBreakClass? _nextClass;

        public LineBreaker(ReadOnlySlice<char> codePoints)
        {
            _codePoints = codePoints;
            _pos = 0;
            _lastPos = 0;
            _curClass = null;
            _nextClass = null;
            CurrentBreak = default;
        }

        public LineBreak CurrentBreak { get; private set; }

        public bool NextBreak()
        {
            // get the first char if we're at the beginning of the string
            if (!_curClass.HasValue)
            {
                if (PeekCharClass() == LineBreakClass.SP)
                {
                    _curClass = LineBreakClass.WJ;
                }
                else
                {
                    _curClass = MapFirst(ReadCharClass());
                }
            }

            while (_pos < _codePoints.Length)
            {
                _lastPos = _pos;
                var lastClass = _nextClass;
                _nextClass = ReadCharClass();

                // explicit newline
                if (_curClass.HasValue && (_curClass == LineBreakClass.BK || _curClass == LineBreakClass.CR && _nextClass != LineBreakClass.LF))
                {
                    _curClass = MapFirst(MapClass(_nextClass.Value));
                    CurrentBreak = new LineBreak(FindPriorNonWhitespace(_lastPos), _lastPos, true);
                    return true;
                }

                // handle classes not handled by the pair table
                LineBreakClass? cur = null;
                switch (_nextClass.Value)
                {
                    case LineBreakClass.SP:
                        cur = _curClass;
                        break;

                    case LineBreakClass.BK:
                    case LineBreakClass.LF:
                    case LineBreakClass.NL:
                        cur = LineBreakClass.BK;
                        break;

                    case LineBreakClass.CR:
                        cur = LineBreakClass.CR;
                        break;

                    case LineBreakClass.CB:
                        cur = LineBreakClass.BA;
                        break;
                }

                if (cur != null)
                {
                    _curClass = cur;

                    if (_nextClass.Value == LineBreakClass.CB)
                    {
                        CurrentBreak = new LineBreak(FindPriorNonWhitespace(_lastPos), _lastPos);
                        return true;
                    }

                    continue;
                }

                // if not handled already, use the pair table
                var shouldBreak = false;
                switch (LineBreakPairTable.Table[(int)_curClass.Value][(int)_nextClass.Value])
                {
                    case LineBreakPairTable.DI_BRK: // Direct break
                        shouldBreak = true;
                        break;

                    case LineBreakPairTable.IN_BRK: // possible indirect break
                        shouldBreak = lastClass.HasValue && lastClass.Value == LineBreakClass.SP;
                        break;

                    case LineBreakPairTable.CI_BRK:
                        shouldBreak = lastClass.HasValue && lastClass.Value == LineBreakClass.SP;
                        if (!shouldBreak)
                        {
                            continue;
                        }
                        break;

                    case LineBreakPairTable.CP_BRK: // prohibited for combining marks
                        if (!lastClass.HasValue || lastClass.Value != LineBreakClass.SP)
                        {
                            continue;
                        }
                        break;
                }

                _curClass = _nextClass;

                if (shouldBreak)
                {
                    CurrentBreak = new LineBreak(FindPriorNonWhitespace(_lastPos), _lastPos);
                    return true;
                }
            }

            if (_pos >= _codePoints.Length)
            {
                if (_lastPos < _codePoints.Length)
                {
                    _lastPos = _codePoints.Length;
                    var cls = UnicodeClasses.LineBreakClass(CodepointReader.Peek(_codePoints, _codePoints.Length - 1, out _));
                    bool required = cls == LineBreakClass.BK || cls == LineBreakClass.LF || cls == LineBreakClass.CR;
                    CurrentBreak = new LineBreak(FindPriorNonWhitespace(_codePoints.Length), _codePoints.Length, required);
                    return true;
                }
            }

            return false;
        }

        private int FindPriorNonWhitespace(int from)
        {
            if (from > 0)
            {
                var cp = CodepointReader.Peek(_codePoints, from - 1, out _);

                var cls = UnicodeClasses.LineBreakClass(cp);

                if (cls == LineBreakClass.BK || cls == LineBreakClass.LF || cls == LineBreakClass.CR)
                {
                    from -= cp <= ushort.MaxValue ? 1 : 2;
                }
            }

            while (from > 0)
            {
                var cp = CodepointReader.Peek(_codePoints, from - 1, out _);

                var cls = UnicodeClasses.LineBreakClass(cp);

                if (cls == LineBreakClass.SP)
                {
                    from -= cp <= ushort.MaxValue ? 1 : 2;
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
            return MapClass(UnicodeClasses.LineBreakClass(CodepointReader.Read(_codePoints, ref _pos)));
        }

        private LineBreakClass PeekCharClass()
        {
            return MapClass(UnicodeClasses.LineBreakClass(CodepointReader.Peek(_codePoints, _pos, out _)));
        }

        private static LineBreakClass MapClass(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.AI:
                    return LineBreakClass.AL;

                case LineBreakClass.SA:
                case LineBreakClass.SG:
                case LineBreakClass.XX:
                    return LineBreakClass.AL;

                case LineBreakClass.CJ:
                    return LineBreakClass.NS;

                default:
                    return c;
            }
        }

        private static LineBreakClass MapFirst(LineBreakClass c)
        {
            switch (c)
            {
                case LineBreakClass.LF:
                case LineBreakClass.NL:
                    return LineBreakClass.BK;

                case LineBreakClass.CB:
                    return LineBreakClass.BA;

                case LineBreakClass.SP:
                    return LineBreakClass.WJ;

                default:
                    return c;
            }
        }
    }
}
