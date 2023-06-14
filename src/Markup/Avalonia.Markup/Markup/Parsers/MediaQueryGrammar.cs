using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Core;
using Avalonia.Styling;
using Avalonia.Utilities;

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

namespace Avalonia.Markup.Parsers
{
    internal static class MediaQueryGrammar
    {
        const string WidthKeyword = "width";
        const string HeightKeyword = "height";
        const string OrientationKeyword = "orientation";
        const string PlatformKeyword = "platform";
        static string[] Keywords = new string[] { WidthKeyword, HeightKeyword, OrientationKeyword, PlatformKeyword };

        private enum State
        {
            Start,
            Left,
            Middle,
            End,
        }

        public static IEnumerable<ISyntax> Parse(string s)
        {
            var r = new CharacterReader(s.AsSpan());
            return Parse(ref r, null);
        }

        private static IEnumerable<ISyntax> Parse(ref CharacterReader r, char? end)
        {
            var state = State.Start;
            var selector = new List<ISyntax>();
            while (!r.End && state != State.End)
            {
                ISyntax? syntax = null;
                switch (state)
                {
                    case State.Start:
                        (state, syntax) = ParseStart(ref r);
                        break;
                    case State.Left:
                        (state, syntax) = ParseLeft(ref r, end);
                        break;
                    case State.Middle:
                        (state, syntax) = ParseMiddle(ref r, end);
                        break;
                }
                if (syntax != null)
                {
                    selector.Add(syntax);
                }
            }

            if (state != State.Start && state != State.Middle && state != State.End)
            {
                throw new ExpressionParseException(r.Position, "Unexpected end of selector");
            }

            return selector;
        }

        private static (State, ISyntax?) ParseLeft(ref CharacterReader r, char? end)
        {
            var leftArgument = r.TakeWhile(x => !char.IsWhiteSpace(x));
            var leftOperator = ParseOperator(ref r);

            // Reverse the operators
            switch (leftOperator)
            {
                case QueryComparisonOperator.LessThan:
                    leftOperator = QueryComparisonOperator.GreaterThan;
                    break;
                case QueryComparisonOperator.GreaterThan:
                    leftOperator = QueryComparisonOperator.LessThan;
                    break;
                case QueryComparisonOperator.LessThanOrEquals:
                    leftOperator = QueryComparisonOperator.GreaterThanOrEquals;
                    break;
                case QueryComparisonOperator.GreaterThanOrEquals:
                    leftOperator = QueryComparisonOperator.LessThanOrEquals;
                    break;
            }

            var position = r.Position;

            var feature = ParseFeature(ref r);

            if(feature == null)
            {
                throw new InvalidOperationException("Invalid syntax found");
            }

            if (feature is not RangeSyntax && leftArgument.Length > 0)
            {
                throw new ExpressionParseException(position, $"Unexpected value.");
            }

            if (feature is RangeSyntax syntax)
            {
                if (double.TryParse(leftArgument.ToString(), out var value))
                {
                    syntax.Left = value;
                    syntax.LeftOperator = leftOperator;
                }
            }

            return (State.Middle, feature);

        }

        private static ISyntax? ParseFeature(ref CharacterReader r)
        {
            r.SkipWhitespace();

            var identifier = r.ParseStyleClass();

            if (identifier.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Expected query feature name.");
            }
            var s = identifier.ToString();
            if (!Keywords.Any(x => s == x))
            {
                throw new InvalidOperationException($"Unknown feature name found: {identifier.ToString()}");
            }

            if (identifier.SequenceEqual(WidthKeyword.AsSpan()))
            {
                var op = ParseOperator(ref r);
                double val = op == QueryComparisonOperator.None ? 0 : ParseDecimal(ref r);

                var syntax = new WidthSyntax()
                {
                    Right = val,
                    RightOperator = op
                };

                return syntax;
            }

            if (identifier.SequenceEqual(HeightKeyword.AsSpan()))
            {
                var op = ParseOperator(ref r);
                double val = op == QueryComparisonOperator.None ? 0 : ParseDecimal(ref r);

                var syntax = new HeightSyntax()
                {
                    Right = val,
                    RightOperator = op
                };

                return syntax;
            }

            if (identifier.SequenceEqual(OrientationKeyword.AsSpan()))
            {
                if(!r.TakeIf(':'))
                    throw new ExpressionParseException(r.Position, "Expected ':' after 'orientation'.");

                var orientation = ParseEnum<MediaOrientation>(ref r);

                var syntax = new OrientationSyntax()
                {
                    Argument = orientation,
                };

                return syntax;
            }

            if (identifier.SequenceEqual(PlatformKeyword.AsSpan()))
            {
                if(!r.TakeIf(':'))
                    throw new ExpressionParseException(r.Position, "Expected ':' after 'platform'.");

                r.SkipWhitespace();

                var platform = ParseString(ref r);

                var syntax = new PlatformSyntax()
                {
                    Argument = platform,
                };

                return syntax;
            }

            return null;
        }

        private static (State, ISyntax?) ParseStart(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.End)
            {
                return (State.End, null);
            }

            if(int.TryParse(r.Peek.ToString(), out _))
            {
                return (State.Left,  null);
            }

            if(char.IsLetter(r.Peek))
            {
                return (State.Middle, ParseFeature(ref r));
            }

            throw new InvalidOperationException("Invalid syntax found");
        }

        private static (State, ISyntax?) ParseMiddle(ref CharacterReader r, char? end)
        {
            r.SkipWhitespace();

            if (r.TakeIf(','))
            {
                return (State.Start, new OrSyntax());
            }
            else if (end.HasValue && !r.End && r.Peek == end.Value)
            {
                return (State.End, null);
            }
            else
            {
                var identifier = r.TakeWhile(c => !char.IsWhiteSpace(c));

                if (identifier.SequenceEqual("and".AsSpan()))
                {
                    return (State.Start, new AndSyntax());
                }
            }
            throw new InvalidOperationException("Invalid syntax found");
        }
        
        private static double ParseDecimal(ref CharacterReader r)
        {
            r.SkipWhitespace();
            var number = r.ParseNumber();
            if (number.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a number after.");
            }

            return double.Parse(number.ToString());
        }
        
        private static QueryComparisonOperator ParseOperator(ref CharacterReader r)
        {
            r.SkipWhitespace();
            var queryOperator = r.TakeWhile(x => !char.IsWhiteSpace(x));

            return queryOperator.ToString() switch
            {
                "=" => QueryComparisonOperator.Equals,
                "<" => QueryComparisonOperator.LessThan,
                ">" => QueryComparisonOperator.GreaterThan,
                "<=" => QueryComparisonOperator.LessThanOrEquals,
                ">=" => QueryComparisonOperator.GreaterThanOrEquals,
                "" => QueryComparisonOperator.None,
                _ => throw new ExpressionParseException(r.Position, $"Expected a comparison operator after.")
            };
        }
        
        private static T ParseEnum<T>(ref CharacterReader r) where T: struct
        {
            var identifier = r.ParseIdentifier();

            if (Enum.TryParse<T>(identifier.ToString(), true, out T value))
                return value;

            throw new ExpressionParseException(r.Position, $"Expected a {typeof(T)} after.");
        }
        
        private static string ParseString(ref CharacterReader r) 
        {
            return r.ParseIdentifier().ToString();
        }

        private static void Expect(ref CharacterReader r, char c)
        {
            if (r.End)
            {
                throw new ExpressionParseException(r.Position, $"Expected '{c}', got end of selector.");
            }
            else if (!r.TakeIf(')'))
            {
                throw new ExpressionParseException(r.Position, $"Expected '{c}', got '{r.Peek}'.");
            }
        }

        public class OrSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is OrSyntax;
            }
        }

        public class AndSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is AndSyntax;
            }
        }

        public class OrientationSyntax : ISyntax
        {
            public MediaOrientation Argument { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is OrientationSyntax orientation) && orientation.Argument == Argument;
            }
        }

        public class PlatformSyntax : ISyntax
        {
            public string Argument { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return (obj is PlatformSyntax orientation) && orientation.Argument == Argument;
            }
        }

        public abstract class QuerySyntax<T> : ISyntax
        {
            public T? Left { get; set; }
            public QueryComparisonOperator LeftOperator { get; set; }
            public T? Right { get; set; }
            public QueryComparisonOperator RightOperator { get; set; }
        }

        public abstract class RangeSyntax : QuerySyntax<double>
        {

        }

        public class WidthSyntax : RangeSyntax
        {
            public override bool Equals(object? obj)
            {
                return (obj is WidthSyntax width) && width.Left == Left 
                    && width.Right == Right
                    && width.LeftOperator == LeftOperator
                    && width.RightOperator == RightOperator;
            }
        }

        public class HeightSyntax : RangeSyntax
        {
            public override bool Equals(object? obj)
            {
                return (obj is HeightSyntax width) && width.Left == Left 
                    && width.Right == Right
                    && width.LeftOperator == LeftOperator
                    && width.RightOperator == RightOperator;
            }
        }
    }
}
