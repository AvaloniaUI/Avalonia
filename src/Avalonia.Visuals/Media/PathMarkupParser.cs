// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Avalonia.Platform;

namespace Avalonia.Media
{
    /// <summary>
    /// Parses a path markup string.
    /// </summary>
    public class PathMarkupParser : IDisposable
    {
        private static readonly string s_separatorPattern;
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

        private IGeometryContext _geometryContext;
        private Point _currentPoint;
        private Point? _previousControlPoint;
        private bool? _isOpen;
        private bool _isDisposed;

        static PathMarkupParser()
        {
            s_separatorPattern = CreatesSeparatorPattern();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PathMarkupParser"/> class.
        /// </summary>
        /// <param name="geometryContext">The geometry context.</param>
        /// <exception cref="ArgumentNullException">geometryContext</exception>
        public PathMarkupParser(IGeometryContext geometryContext)
        {
            if (geometryContext == null)
            {
                throw new ArgumentNullException(nameof(geometryContext));
            }

            _geometryContext = geometryContext;
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

        /// <summary>
        /// Parses the specified path data and writes the result to the geometryContext of this instance.
        /// </summary>
        /// <param name="pathData">The path data.</param>
        public void Parse(string pathData)
        {
            var tokens = ParseTokens2(pathData);

            CreateGeometry(tokens);
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
                _geometryContext = null;
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

        private static IEnumerable<CommandToken> ParseTokens2(string s)
        {
            var commands = new List<CommandToken>();
            var span = s.AsSpan();
            while (!span.IsEmpty)
            {
                commands.Add(CommandToken.Parse(ref span));
            }
            return commands;
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

        private void CreateGeometry(IEnumerable<CommandToken> commandTokens)
        {
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

            if (_isOpen != null)
            {
                _geometryContext.EndFigure(false);
            }
        }

        private void CreateFigure()
        {
            if (_isOpen != null)
            {
                _geometryContext.EndFigure(false);
            }

            _geometryContext.BeginFigure(_currentPoint);

            _isOpen = true;
        }

        private void SetFillRule(CommandToken commandToken)
        {
            var fillRule = commandToken.ReadFillRule();

            _geometryContext.SetFillRule(fillRule);
        }

        private void CloseFigure()
        {
            if (_isOpen == true)
            {
                _geometryContext.EndFigure(true);
            }

            _previousControlPoint = null;

            _isOpen = null;
        }

        private void AddMove(CommandToken commandToken)
        {
            var currentPoint = commandToken.IsRelative
                                   ? commandToken.ReadRelativePoint(_currentPoint)
                                   : commandToken.ReadPoint();

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

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.LineTo(_currentPoint);
        }

        private void AddHorizontalLine(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                ? new Point(_currentPoint.X + commandToken.ReadDouble(), _currentPoint.Y)
                                : _currentPoint.WithX(commandToken.ReadDouble());

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.LineTo(_currentPoint);
        }

        private void AddVerticalLine(CommandToken commandToken)
        {
            _currentPoint = commandToken.IsRelative
                                ? new Point(_currentPoint.X, _currentPoint.Y + commandToken.ReadDouble())
                                : _currentPoint.WithY(commandToken.ReadDouble());

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.LineTo(_currentPoint);
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

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.CubicBezierTo(point1, point2, point3);

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

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.QuadraticBezierTo(start, end);

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

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.CubicBezierTo(_previousControlPoint ?? _currentPoint, point2, end);

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

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.QuadraticBezierTo(_previousControlPoint ?? _currentPoint, end);

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

            if (_isOpen == null)
            {
                CreateFigure();
            }

            _geometryContext.ArcTo(end, size, rotationAngle, isLargeArc, sweepDirection);

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
            
            public static CommandToken Parse(ref ReadOnlySpan<char> span)
            {
                if (!ReadCommand(ref span, out var command, out var isRelative))
                {
                    throw new InvalidDataException("No path command declared.");
                }

                span = span.Slice(1);
                span = SkipWhitespace(span);

                var arguments = new List<string>();

                while (ReadArgument(ref span, out var argument))
                {
                    arguments.Add(argument.ToString());
                    span = ReadSeparator(span);
                }

                return new CommandToken(command, isRelative, arguments);
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

            private static bool ReadCommand(ref ReadOnlySpan<char> span, out Command command, out bool relative)
            {
                span = SkipWhitespace(span);
                if (span.IsEmpty)
                {
                    command = default;
                    relative = false;
                    return false;
                }

                var c = span[0];

                if (!s_commands.TryGetValue(char.ToUpperInvariant(c), out command))
                {
                    throw new InvalidDataException("Unexpected path command '" + c + "'.");
                }

                relative = char.IsLower(c);

                return true;
            }

            private static bool ReadCommand(TextReader reader, ref Command command, ref bool relative)
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

            private static bool ReadArgument(ref ReadOnlySpan<char> remaining, out ReadOnlySpan<char> argument)
            {
                if (remaining.IsEmpty)
                {
                    argument = ReadOnlySpan<char>.Empty;
                    return false;
                }

                var valid = false;
                int i = 0;
                if (remaining[i] == '-')
                {
                    i++;
                }
                for (; i < remaining.Length && char.IsNumber(remaining[i]); i++) valid = true ;

                if (i < remaining.Length && remaining[i] == '.')
                {
                    valid = false;
                    i++;
                }
                for (; i < remaining.Length && char.IsNumber(remaining[i]); i++) valid = true ;

                if (!valid)
                {
                    argument = ReadOnlySpan<char>.Empty;
                    return false;
                }
                argument = remaining.Slice(0, i);
                remaining = remaining.Slice(i);
                return true;
            }

            private static ReadOnlySpan<char> ReadSeparator(ReadOnlySpan<char> span)
            {
                span = SkipWhitespace(span);
                if (!span.IsEmpty && span[0] == ',')
                {
                    span = span.Slice(1);
                }
                return SkipWhitespace(span);
            }
            
            private static ReadOnlySpan<char> SkipWhitespace(ReadOnlySpan<char> span)
            {
                int i = 0;
                for (; i < span.Length && char.IsWhiteSpace(span[i]); i++) ;
                return span.Slice(i);
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