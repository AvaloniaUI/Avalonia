// -----------------------------------------------------------------------
// <copyright file="PathMarkupParser.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

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

        private StreamGeometry geometry;

        private StreamGeometryContext context;

        public PathMarkupParser(StreamGeometry geometry, StreamGeometryContext context)
        {
            this.geometry = geometry;
            this.context = context;
        }

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
                                this.context.EndFigure(false);
                            }

                            point = ReadPoint(reader);
                            this.context.BeginFigure(point, true);
                            openFigure = true;
                            break;

                        case Command.Line:
                            point = ReadPoint(reader);
                            this.context.LineTo(point);
                            break;

                        case Command.LineRelative:
                            point = ReadRelativePoint(reader, point);
                            this.context.LineTo(point);
                            break;

                        ////case Command.HorizontalLine:
                        ////    point.X = ReadDouble(reader);
                        ////    this.context.LineTo(point, true, false);
                        ////    break;

                        ////case Command.HorizontalLineRelative:
                        ////    point.X += ReadDouble(reader);
                        ////    this.context.LineTo(point, true, false);
                        ////    break;

                        ////case Command.VerticalLine:
                        ////    point.Y = ReadDouble(reader);
                        ////    this.context.LineTo(point, true, false);
                        ////    break;

                        ////case Command.VerticalLineRelative:
                        ////    point.Y += ReadDouble(reader);
                        ////    this.context.LineTo(point, true, false);
                        ////    break;

                        ////case Command.CubicBezierCurve:
                        ////{
                        ////    Point point1 = ReadPoint(reader);
                        ////    Point point2 = ReadPoint(reader);
                        ////    point = ReadPoint(reader);
                        ////    this.context.BezierTo(point1, point2, point, true, false);
                        ////    break;
                        ////}

                        case Command.Close:
                            this.context.EndFigure(true);
                            openFigure = false;
                            break;

                        default:
                            throw new NotSupportedException("Unsupported command");
                    }

                    lastCommand = command;
                }

                if (openFigure)
                {
                    this.context.EndFigure(false);
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

            return double.Parse(b.ToString());
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
