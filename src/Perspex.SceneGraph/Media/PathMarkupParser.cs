// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Perspex.Media
{
    /// <summary>
    /// Parses a path markup string.
    /// </summary>
    public class PathMarkupParser
    {
        private static readonly Dictionary<char, Command> Commands = new Dictionary<char, Command>
        {
            { 'F', Command.FillRule },
            { 'f', Command.FillRule },
            { 'M', Command.Move },
            { 'm', Command.MoveRelative },
            { 'L', Command.Line },
            { 'l', Command.LineRelative },
            { 'H', Command.HorizontalLine },
            { 'h', Command.HorizontalLineRelative },
            { 'V', Command.VerticalLine },
            { 'v', Command.VerticalLineRelative },
            { 'C', Command.CubicBezierCurve },
            { 'c', Command.CubicBezierCurveRelative },
            { 'Z', Command.Close },
            { 'z', Command.Close },
        };

        private StreamGeometry _geometry;

        private readonly StreamGeometryContext _context;

        /// <summary>
        /// Initializes a new instance of the <see cref="PathMarkupParser"/> class.
        /// </summary>
        /// <param name="geometry">The geometry in which the path should be stored.</param>
        /// <param name="context">The context for <paramref name="geometry"/>.</param>
        public PathMarkupParser(StreamGeometry geometry, StreamGeometryContext context)
        {
            _geometry = geometry;
            _context = context;
        }

        /// <summary>
        /// Defines the command currently being processed.
        /// </summary>
        private enum Command
        {
            None,
            FillRule,
            Move,
            MoveRelative,
            Line,
            LineRelative,
            HorizontalLine,
            HorizontalLineRelative,
            VerticalLine,
            VerticalLineRelative,
            CubicBezierCurve,
            CubicBezierCurveRelative,
            Close,
            Eof,
        }

        /// <summary>
        /// Parses the specified markup string.
        /// </summary>
        /// <param name="s">The markup string.</param>
        public void Parse(string s)
        {
            bool openFigure = false;

            using (StringReader reader = new StringReader(s))
            {
                Command lastCommand = Command.None;
                Command command;
                Point point = new Point();

                while ((command = ReadCommand(reader, lastCommand)) != Command.Eof)
                {
                    switch (command)
                    {
                        case Command.FillRule:
                            // TODO: Implement.
                            reader.Read();
                            break;

                        case Command.Move:
                        case Command.MoveRelative:
                            if (openFigure)
                            {
                                _context.EndFigure(false);
                            }

                            point = command == Command.Move ? 
                                ReadPoint(reader) : 
                                ReadRelativePoint(reader, point);

                            _context.BeginFigure(point, true);
                            openFigure = true;
                            break;

                        case Command.Line:
                            point = ReadPoint(reader);
                            _context.LineTo(point);
                            break;

                        case Command.LineRelative:
                            point = ReadRelativePoint(reader, point);
                            _context.LineTo(point);
                            break;

                        case Command.HorizontalLine:
                            point = point.WithX(ReadDouble(reader));
                            _context.LineTo(point);
                            break;

                        case Command.HorizontalLineRelative:
                            point = new Point(point.X + ReadDouble(reader), point.Y);
                            _context.LineTo(point);
                            break;

                        case Command.VerticalLine:
                            point = point.WithY(ReadDouble(reader));
                            _context.LineTo(point);
                            break;

                        case Command.VerticalLineRelative:
                            point = new Point(point.X, point.Y + ReadDouble(reader));
                            _context.LineTo(point);
                            break;

                        case Command.CubicBezierCurve:
                            {
                                Point point1 = ReadPoint(reader);
                                Point point2 = ReadPoint(reader);
                                point = ReadPoint(reader);
                                _context.CubicBezierTo(point1, point2, point);
                                break;
                            }

                        case Command.Close:
                            _context.EndFigure(true);
                            openFigure = false;
                            break;

                        default:
                            throw new NotSupportedException("Unsupported command");
                    }

                    lastCommand = command;
                }

                if (openFigure)
                {
                    _context.EndFigure(false);
                }
            }
        }

        private static Command ReadCommand(StringReader reader, Command lastCommand)
        {
            ReadWhitespace(reader);

            int i = reader.Peek();

            if (i == -1)
            {
                return Command.Eof;
            }
            else
            {
                char c = (char)i;
                Command command = Command.None;

                if (!Commands.TryGetValue(c, out command))
                {
                    if ((char.IsDigit(c) || c == '.' || c == '+' || c == '-') &&
                        (lastCommand != Command.None))
                    {
                        return lastCommand;
                    }
                    else
                    {
                        throw new InvalidDataException("Unexpected path command '" + c + "'.");
                    }
                }

                reader.Read();
                return command;
            }
        }

        private static double ReadDouble(TextReader reader)
        {
            // TODO: Handle Infinity, NaN and scientific notation.
            StringBuilder b = new StringBuilder();
            bool readSign = false;
            bool readPoint = false;
            bool readExponent = false;
            int i;

            while ((i = reader.Peek()) != -1)
            {
                char c = char.ToUpperInvariant((char)i);

                if (((c == '+' || c == '-') && !readSign) ||
                    (c == '.' && !readPoint) ||
                    (c == 'E' && !readExponent) ||
                    char.IsDigit(c))
                {
                    b.Append(c);
                    reader.Read();
                    readSign = c == '+' || c == '-';
                    readPoint = c == '.';

                    if (c == 'E')
                    {
                        readSign = false;
                        readExponent = c == 'E';
                    }
                }
                else
                {
                    break;
                }
            }

            return double.Parse(b.ToString(), CultureInfo.InvariantCulture);
        }

        private static Point ReadPoint(StringReader reader)
        {
            ReadWhitespace(reader);
            double x = ReadDouble(reader);
            ReadSeparator(reader);
            double y = ReadDouble(reader);
            return new Point(x, y);
        }

        private static Point ReadRelativePoint(StringReader reader, Point lastPoint)
        {
            ReadWhitespace(reader);
            double x = ReadDouble(reader);
            ReadSeparator(reader);
            double y = ReadDouble(reader);
            return new Point(lastPoint.X + x, lastPoint.Y + y);
        }

        private static void ReadSeparator(StringReader reader)
        {
            int i;
            bool readComma = false;

            while ((i = reader.Peek()) != -1)
            {
                char c = (char)i;

                if (char.IsWhiteSpace(c))
                {
                    reader.Read();
                }
                else if (c == ',')
                {
                    if (readComma)
                    {
                        throw new InvalidDataException("Unexpected ','.");
                    }

                    readComma = true;
                    reader.Read();
                }
                else
                {
                    break;
                }
            }
        }

        private static void ReadWhitespace(StringReader reader)
        {
            int i;

            while ((i = reader.Peek()) != -1)
            {
                char c = (char)i;

                if (char.IsWhiteSpace(c))
                {
                    reader.Read();
                }
                else
                {
                    break;
                }
            }
        }
    }
}
