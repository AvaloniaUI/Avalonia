using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using Avalonia.Media;

namespace Avalonia.Visuals.Media
{
    public enum Command
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

    public class SvgParser
    {
        private static readonly string s_separatorPattern;
        private readonly StreamGeometryContext _context;
        private Point _currentPoint;
        private Point? _previousControlPoint;
        private bool _openFigure;

        private static readonly Dictionary<char, Command> Commands =
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

        static SvgParser()
        {
            s_separatorPattern = CreatesSeparatorPattern();
        }

        public SvgParser(StreamGeometryContext context)
        {
            _context = context;
        }

        public void Parse(string s)
        {
            var tokens = ParseTokens(s);

            foreach (var commandToken in tokens)
            {
                ExecuteCommand(commandToken);
            }
        }

        private static string CreatesSeparatorPattern()
        {
            var stringBuilder = new StringBuilder();

            foreach (var command in Commands.Keys)
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

        private void ExecuteCommand(CommandToken commandToken)
        {
            while (true)
            {
                switch (commandToken.Command)
                {
                    case Command.None:
                        break;
                    case Command.FillRule:
                        ExecuteFillRuleCommand(commandToken);
                        break;
                    case Command.Move:
                        ExecuteMoveCommand(commandToken);
                        break;
                    case Command.Line:
                        ExecuteLineCommand(commandToken);
                        break;
                    case Command.HorizontalLine:
                        ExecuteHorizontalLineCommand(commandToken);
                        break;
                    case Command.VerticalLine:
                        ExecuteVerticalLineCommand(commandToken);
                        break;
                    case Command.CubicBezierCurve:
                        ExecuteCubicBezierCurve(commandToken);
                        break;
                    case Command.QuadraticBezierCurve:
                        ExecuteQuadraticBezierCurve(commandToken);
                        break;
                    case Command.SmoothCubicBezierCurve:
                        ExecuteSmoothCubicBezierCurve(commandToken);
                        break;
                    case Command.SmoothQuadraticBezierCurve:
                        ExecuteSmoothQuadraticBezierCurveCommand(commandToken);
                        break;
                    case Command.Arc:
                        ExecuteArcCommand(commandToken);
                        break;
                    case Command.Close:
                        ExecuteCloseCommand();
                        break;
                    default:
                        throw new NotSupportedException("Unsupported command");
                }

                if (commandToken.CurrentPosition < commandToken.Arguments.Count - 1)
                {
                    continue;
                }

                break;
            }
        }

        private void ExecuteFillRuleCommand(CommandToken commandToken)
        {
            _context.SetFillRule(commandToken.ReadFillRule());

            _previousControlPoint = null;
        }

        private void ExecuteMoveCommand(CommandToken commandToken)
        {
            if (_openFigure)
            {
                _context.EndFigure(false);
            }

            _currentPoint = commandToken.IsRelative
                                ? commandToken.ReadRelativePoint(_currentPoint)
                                : commandToken.ReadPoint();

            _context.BeginFigure(_currentPoint, true);

            _openFigure = true;

            _previousControlPoint = null;
        }

        private void ExecuteLineCommand(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                ? commandToken.ReadRelativePoint(_currentPoint)
                                : commandToken.ReadPoint();

            _context.LineTo(_currentPoint);

            _previousControlPoint = null;
        }

        private void ExecuteHorizontalLineCommand(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                     ? new Point(_currentPoint.X + commandToken.ReadDouble(), _currentPoint.Y)
                                     : _currentPoint.WithX(commandToken.ReadDouble());

            _context.LineTo(_currentPoint);

            _previousControlPoint = null;
        }

        private void ExecuteVerticalLineCommand(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                     ? new Point(_currentPoint.X, _currentPoint.Y + commandToken.ReadDouble())
                                     : _currentPoint.WithY(commandToken.ReadDouble());

            _context.LineTo(_currentPoint);

            _previousControlPoint = null;
        }

        private void ExecuteCubicBezierCurve(CommandToken commandToken)
        {
            var end = commandToken.IsRelative
                                ? commandToken.ReadRelativePoint(_currentPoint)
                                : commandToken.ReadPoint();

            if (_previousControlPoint != null)
            {
                _previousControlPoint = MirrorControlPoint((Point)_previousControlPoint, _currentPoint);
            }

            _context.QuadraticBezierTo(_previousControlPoint ?? _currentPoint, end);

            _currentPoint = end;
        }

        private void ExecuteQuadraticBezierCurve(CommandToken commandToken)
        {
            var end = commandToken.IsRelative
                          ? commandToken.ReadRelativePoint(_currentPoint)
                          : commandToken.ReadPoint();

            if (_previousControlPoint != null)
            {
                _previousControlPoint = MirrorControlPoint((Point)_previousControlPoint, _currentPoint);
            }

            _context.QuadraticBezierTo(_previousControlPoint ?? _currentPoint, end);

            _currentPoint = end;
        }

        private void ExecuteSmoothCubicBezierCurve(CommandToken commandToken)
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

            _context.CubicBezierTo(_previousControlPoint ?? _currentPoint, point2, end);

            _previousControlPoint = point2;

            _currentPoint = end;
        }

        private void ExecuteSmoothQuadraticBezierCurveCommand(CommandToken commandToken)
        {
            var end = commandToken.IsRelative
                          ? commandToken.ReadRelativePoint(_currentPoint)
                          : commandToken.ReadPoint();

            if (_previousControlPoint != null)
            {
                _previousControlPoint = MirrorControlPoint((Point)_previousControlPoint, _currentPoint);
            }

            _context.QuadraticBezierTo(_previousControlPoint ?? _currentPoint, end);

            _currentPoint = end;
        }

        private void ExecuteArcCommand(CommandToken commandToken)
        {
            var size = commandToken.ReadSize();

            var rotationAngle = commandToken.ReadDouble();

            var isLargeArc = commandToken.ReadBool();

            var sweepDirection = commandToken.ReadBool() ? SweepDirection.Clockwise : SweepDirection.CounterClockwise;

            _currentPoint = commandToken.IsRelative
                                     ? new Point(_currentPoint.X, _currentPoint.Y + commandToken.ReadDouble())
                                     : _currentPoint.WithY(commandToken.ReadDouble());

            _context.ArcTo(_currentPoint, size, rotationAngle, isLargeArc, sweepDirection);

            _previousControlPoint = null;
        }

        private void ExecuteCloseCommand()
        {
            _context.EndFigure(true);

            _openFigure = false;

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

            public int CurrentPosition { get; private set;}

            public List<string> Arguments { get; }

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
                StringReader reader,
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

                if (!Commands.TryGetValue(char.ToUpperInvariant(c), out var next))
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
