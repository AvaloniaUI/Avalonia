// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Avalonia.Media
{
    /// <summary>
    /// Parses a path markup string.
    /// </summary>
    public class PathMarkupParser : IDisposable
    {
        private static readonly string s_separatorPattern;

        private Point _currentPoint;
        private Point? _previousControlPoint;
        private PathGeometry _currentGeometry;
        private PathFigure _currentFigure;
        private bool _isDisposed;

        private static readonly Dictionary<char, Command> s_commands =
            new Dictionary<char, Command>
                {
                    { 'F', Command.FillRule },
                    { 'M', Command.Move },
                    { 'L', Command.Line },
                    { 'H', Command.HorizontalLine },
                    { 'V', Command.VerticalLine },
                    { 'Q', Command.QuadraticBezierCurve },
                    { 'T', Command.SmoothQuadraticBezierCurve },
                    { 'C', Command.CubicBezierCurve },
                    { 'S', Command.SmoothCubicBezierCurve },
                    { 'A', Command.Arc },
                    { 'Z', Command.Close },
                };

        static PathMarkupParser()
        {
            s_separatorPattern = CreatesSeparatorPattern();
        }

        private enum Command
        {
            None,
            FillRule,
            Move,
            Line,
            HorizontalLine,
            VerticalLine,
            CubicBezierCurve,
            QuadraticBezierCurve,
            SmoothCubicBezierCurve,
            SmoothQuadraticBezierCurve,
            Arc,
            Close
        }

        public PathGeometry Parse(string s)
        {
            _currentGeometry = new PathGeometry();

            var tokens = ParseTokens(s);

            return CreateGeometry(tokens);
        }

        void IDisposable.Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _currentFigure = null;

                _currentGeometry = null;
            }

            _isDisposed = true;
        }

        private static string CreatesSeparatorPattern()
        {
            var stringBuilder = new StringBuilder();

            foreach (var command in s_commands.Keys)
            {
                stringBuilder.Append(command);

                stringBuilder.Append(char.ToLower(command));
            }

            return @"(?=[" + stringBuilder + "])";
        }

        private static IEnumerable<CommandToken> ParseTokens(string s)
        {
            return Regex.Split(s, s_separatorPattern).Where(t => !string.IsNullOrEmpty(t)).Select(CommandToken.Parse);
        }

        private static Point MirrorControlPoint(Point controlPoint, Point center)
        {
            var dir = controlPoint - center;

            return center + -dir;
        }

        private PathGeometry CreateGeometry(IEnumerable<CommandToken> commandTokens)
        {
            _currentGeometry = new PathGeometry();

            _currentPoint = new Point();

            foreach (var commandToken in commandTokens)
            {
                try
                {
                    while (true)
                    {
                        switch (commandToken.Command)
                        {
                            case Command.None:
                                break;
                            case Command.FillRule:
                                SetFillRule(commandToken);
                                break;
                            case Command.Move:
                                AddMove(commandToken);
                                break;
                            case Command.Line:
                                AddLine(commandToken);
                                break;
                            case Command.HorizontalLine:
                                AddHorizontalLine(commandToken);
                                break;
                            case Command.VerticalLine:
                                AddVerticalLine(commandToken);
                                break;
                            case Command.CubicBezierCurve:
                                AddCubicBezierCurve(commandToken);
                                break;
                            case Command.QuadraticBezierCurve:
                                AddQuadraticBezierCurve(commandToken);
                                break;
                            case Command.SmoothCubicBezierCurve:
                                AddSmoothCubicBezierCurve(commandToken);
                                break;
                            case Command.SmoothQuadraticBezierCurve:
                                AddSmoothQuadraticBezierCurve(commandToken);
                                break;
                            case Command.Arc:
                                AddArc(commandToken);
                                break;
                            case Command.Close:
                                CloseFigure();
                                break;
                            default:
                                throw new NotSupportedException("Unsupported command");
                        }

                        if (commandToken.HasImplicitCommands)
                        {
                            continue;
                        }

                        break;
                    }
                }
                catch (InvalidDataException)
                {
                    break;
                }
                catch (NotSupportedException)
                {
                    break;
                }
            }

            return _currentGeometry;
        }

        private void SetFillRule(CommandToken commandToken)
        {
            _currentGeometry.FillRule = commandToken.ReadFillRule();
        }

        private void CloseFigure()
        {
            if (_currentFigure != null && !_currentFigure.IsClosed)
            {
                _currentFigure.IsClosed = true;
            }

            _previousControlPoint = null;

            _currentFigure = null;
        }

        private void CreateFigure()
        {
            _currentFigure = new PathFigure
            {
                StartPoint = _currentPoint,
                IsClosed = false
            };

            _currentGeometry.Figures.Add(_currentFigure);
        }

        private void AddSegment(PathSegment segment)
        {
            if (_currentFigure == null)
            {
                CreateFigure();
            }

            _currentFigure.Segments.Add(segment);
        }

        private void AddMove(CommandToken commandToken)
        {
            var currentPoint = commandToken.ReadPoint();

            _currentPoint = currentPoint;

            CreateFigure();

            if (!commandToken.HasImplicitCommands)
            {
                return;
            }

            while (commandToken.HasImplicitCommands)
            {
                AddLine(commandToken);

                if (commandToken.IsRelative)
                {
                    continue;
                }

                _currentPoint = currentPoint;

                CreateFigure();
            }
        }

        private void AddLine(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                ? commandToken.ReadRelativePoint(_currentPoint)
                                : commandToken.ReadPoint();

            var lineSegment = new LineSegment
            {
                Point = _currentPoint
            };

            AddSegment(lineSegment);
        }

        private void AddHorizontalLine(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                     ? new Point(_currentPoint.X + commandToken.ReadDouble(), _currentPoint.Y)
                                     : _currentPoint.WithX(commandToken.ReadDouble());

            var lineSegment = new LineSegment
            {
                Point = _currentPoint
            };

            AddSegment(lineSegment);
        }

        private void AddVerticalLine(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                     ? new Point(_currentPoint.X, _currentPoint.Y + commandToken.ReadDouble())
                                     : _currentPoint.WithY(commandToken.ReadDouble());

            var lineSegment = new LineSegment
            {
                Point = _currentPoint
            };

            AddSegment(lineSegment);
        }

        private void AddCubicBezierCurve(CommandToken commandToken)
        {
            var point1 = commandToken.IsRelative
                               ? commandToken.ReadRelativePoint(_currentPoint)
                               : commandToken.ReadPoint();

            var point2 = commandToken.IsRelative
                               ? commandToken.ReadRelativePoint(_currentPoint)
                               : commandToken.ReadPoint();

            _previousControlPoint = point2;

            var point3 = commandToken.IsRelative
                        ? commandToken.ReadRelativePoint(_currentPoint)
                        : commandToken.ReadPoint();

            var bezierSegment = new BezierSegment
            {
                Point1 = point1,
                Point2 = point2,
                Point3 = point3
            };

            AddSegment(bezierSegment);

            _currentPoint = point3;
        }

        private void AddQuadraticBezierCurve(CommandToken commandToken)
        {
            var start = commandToken.IsRelative
                          ? commandToken.ReadRelativePoint(_currentPoint)
                          : commandToken.ReadPoint();

            _previousControlPoint = start;

            var end = commandToken.IsRelative
                            ? commandToken.ReadRelativePoint(_currentPoint)
                            : commandToken.ReadPoint();

            var quadraticBezierSegment = new QuadraticBezierSegment
            {
                Point1 = start,
                Point2 = end
            };

            AddSegment(quadraticBezierSegment);

            _currentPoint = end;
        }

        private void AddSmoothCubicBezierCurve(CommandToken commandToken)
        {
            var point2 = commandToken.IsRelative
                          ? commandToken.ReadRelativePoint(_currentPoint)
                          : commandToken.ReadPoint();

            var end = commandToken.IsRelative
                          ? commandToken.ReadRelativePoint(_currentPoint)
                          : commandToken.ReadPoint();

            if (_previousControlPoint != null)
            {
                _previousControlPoint = MirrorControlPoint((Point)_previousControlPoint, _currentPoint);
            }

            var bezierSegment =
                new BezierSegment { Point1 = _previousControlPoint ?? _currentPoint, Point2 = point2, Point3 = end };

            AddSegment(bezierSegment);

            _previousControlPoint = point2;

            _currentPoint = end;
        }

        private void AddSmoothQuadraticBezierCurve(CommandToken commandToken)
        {
            var end = commandToken.IsRelative
                          ? commandToken.ReadRelativePoint(_currentPoint)
                          : commandToken.ReadPoint();

            if (_previousControlPoint != null)
            {
                _previousControlPoint = MirrorControlPoint((Point)_previousControlPoint, _currentPoint);
            }

            var quadraticBezierSegment = new QuadraticBezierSegment
            {
                Point1 = _previousControlPoint ?? _currentPoint,
                Point2 = end
            };

            AddSegment(quadraticBezierSegment);

            _currentPoint = end;
        }

        private void AddArc(CommandToken commandToken)
        {
            var size = commandToken.ReadSize();

            var rotationAngle = commandToken.ReadDouble();

            var isLargeArc = commandToken.ReadBool();

            var sweepDirection = commandToken.ReadBool() ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;

            var end = commandToken.IsRelative
                          ? commandToken.ReadRelativePoint(_currentPoint)
                          : commandToken.ReadPoint();

            var arcSegment = new ArcSegment
            {
                Size = size,
                RotationAngle = rotationAngle,
                IsLargeArc = isLargeArc,
                SweepDirection = sweepDirection,
                Point = end
            };

            AddSegment(arcSegment);

            _currentPoint = end;

            _previousControlPoint = null;
        }

        private class CommandToken
        {
            private const string ArgumentExpression = @"-?[0-9]*\.?\d+";

            private CommandToken(Command command, bool isRelative, IEnumerable<string> arguments)
            {
                Command = command;

                IsRelative = isRelative;

                Arguments = new List<string>(arguments);
            }

            public Command Command { get; }

            public bool IsRelative { get; }

            public bool HasImplicitCommands
            {
                get
                {
                    if (CurrentPosition == 0 && Arguments.Count > 0)
                    {
                        return true;
                    }

                    return CurrentPosition < Arguments.Count - 1;
                }
            }

            private int CurrentPosition { get; set; }

            private List<string> Arguments { get; }

            public static CommandToken Parse(string s)
            {
                using (var reader = new StringReader(s))
                {
                    var command = Command.None;

                    var isRelative = false;

                    if (!ReadCommand(reader, ref command, ref isRelative))
                    {
                        throw new InvalidDataException("No path command declared.");
                    }

                    var commandArguments = reader.ReadToEnd();

                    var argumentMatches = Regex.Matches(commandArguments, ArgumentExpression);

                    var arguments = new List<string>();

                    foreach (Match match in argumentMatches)
                    {
                        arguments.Add(match.Value);
                    }

                    return new CommandToken(command, isRelative, arguments);
                }
            }

            public FillRule ReadFillRule()
            {
                if (CurrentPosition == Arguments.Count)
                {
                    throw new InvalidDataException("Invalid fill rule");
                }

                var value = Arguments[CurrentPosition];

                CurrentPosition++;

                switch (value)
                {
                    case "0":
                        {
                            return FillRule.EvenOdd;
                        }

                    case "1":
                        {
                            return FillRule.NonZero;
                        }

                    default:
                        throw new InvalidDataException("Invalid fill rule");
                }
            }

            public bool ReadBool()
            {
                if (CurrentPosition == Arguments.Count)
                {
                    throw new InvalidDataException("Invalid boolean value");
                }

                var value = Arguments[CurrentPosition];

                CurrentPosition++;

                switch (value)
                {
                    case "1":
                        {
                            return true;
                        }

                    case "0":
                        {
                            return false;
                        }

                    default:
                        throw new InvalidDataException("Invalid boolean value");
                }
            }

            public double ReadDouble()
            {
                if (CurrentPosition == Arguments.Count)
                {
                    throw new InvalidDataException("Invalid double value");
                }

                var value = Arguments[CurrentPosition];

                CurrentPosition++;

                return double.Parse(value, CultureInfo.InvariantCulture);
            }

            public Size ReadSize()
            {
                var width = ReadDouble();

                var height = ReadDouble();

                return new Size(width, height);
            }

            public Point ReadPoint()
            {
                var x = ReadDouble();

                var y = ReadDouble();

                return new Point(x, y);
            }

            public Point ReadRelativePoint(Point origin)
            {
                var x = ReadDouble();

                var y = ReadDouble();

                return new Point(origin.X + x, origin.Y + y);
            }

            private static bool ReadCommand(
                TextReader reader,
                ref Command command,
                ref bool relative)
            {
                ReadWhitespace(reader);

                var i = reader.Peek();

                if (i == -1)
                {
                    return false;
                }

                var c = (char)i;

                if (!s_commands.TryGetValue(char.ToUpperInvariant(c), out var next))
                {
                    throw new InvalidDataException("Unexpected path command '" + c + "'.");
                }

                command = next;

                relative = char.IsLower(c);

                reader.Read();

                return true;
            }

            private static void ReadWhitespace(TextReader reader)
            {
                int i;

                while ((i = reader.Peek()) != -1)
                {
                    var c = (char)i;

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
}