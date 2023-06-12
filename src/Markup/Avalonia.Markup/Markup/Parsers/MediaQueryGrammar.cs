using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Core;
using Avalonia.Platform;
using Avalonia.Utilities;

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

namespace Avalonia.Markup.Parsers
{
    internal static class MediaQueryGrammar
    {
        private enum State
        {
            Start,
            Middle,
            Colon,
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
                    case State.Middle:
                        (state, syntax) = ParseMiddle(ref r, end);
                        break;
                    case State.Colon:
                        (state, syntax) = ParseColon(ref r);
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

        private static (State, ISyntax?) ParseStart(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.End)
            {
                return (State.End, null);
            }

            if (r.TakeIf(':'))
            {
                return (State.Colon, null);
            }

            throw new InvalidOperationException("Invalid syntax found");
        }

        private static (State, ISyntax?) ParseMiddle(ref CharacterReader r, char? end)
        {
            if (r.TakeIf(':'))
            {
                return (State.Colon, null);
            }
            else if (r.TakeIf(','))
            {
                return (State.Start, new CommaSyntax());
            }
            else if (end.HasValue && !r.End && r.Peek == end.Value)
            {
                return (State.End, null);
            }
            throw new InvalidOperationException("Invalid syntax found");
        }

        private static (State, ISyntax) ParseColon(ref CharacterReader r)
        {
            var identifier = r.ParseStyleClass();

            if (identifier.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Expected class name, is, nth-child or nth-last-child selector after ':'.");
            }

            const string MinWidthKeyword = "min-width";
            const string MaxWidthKeyword = "max-width";
            const string MinHeightKeyword = "min-height";
            const string MaxHeightKeyword = "max-height";
            const string OrientationKeyword = "orientation";
            const string IsOsKeyword = "is-os";

            if(identifier.SequenceEqual(MinWidthKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = ParseDecimal(ref r);
                Expect(ref r, ')');

                var syntax = new MinWidthSyntax { Argument = argument };
                return (State.Middle, syntax);
            }
            if(identifier.SequenceEqual(MaxWidthKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = ParseDecimal(ref r);
                Expect(ref r, ')');

                var syntax = new MaxWidthSyntax { Argument = argument };
                return (State.Middle, syntax);
            }
            if(identifier.SequenceEqual(MinHeightKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = ParseDecimal(ref r);
                Expect(ref r, ')');

                var syntax = new MinHeightSyntax { Argument = argument };
                return (State.Middle, syntax);
            }
            if(identifier.SequenceEqual(MaxHeightKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = ParseDecimal(ref r);
                Expect(ref r, ')');

                var syntax = new MaxHeightSyntax { Argument = argument };
                return (State.Middle, syntax);
            }
            if (identifier.SequenceEqual(OrientationKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = ParseEnum<DeviceOrientation>(ref r);
                Expect(ref r, ')');

                var syntax = new OrientationSyntax { Argument = argument };
                return (State.Middle, syntax);
            }
            if (identifier.SequenceEqual(IsOsKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = ParseString(ref r);
                Expect(ref r, ')');

                var syntax = new IsOsSyntax { Argument = argument };
                return (State.Middle, syntax);

            }

            throw new InvalidOperationException("Invalid syntax found");
        }
        
        private static double ParseDecimal(ref CharacterReader r)
        {
            var number = r.ParseNumber();
            if (number.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a number after.");
            }

            return double.Parse(number.ToString());
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

        public class CommaSyntax : ISyntax
        {
            public override bool Equals(object? obj)
            {
                return obj is CommaSyntax;
            }
        }

        public class OrientationSyntax : ISyntax
        {
            public DeviceOrientation Argument { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is OrientationSyntax orientation) && orientation.Argument == Argument;
            }
        }

        public class IsOsSyntax : ISyntax
        {
            public string Argument { get; set; } = string.Empty;

            public override bool Equals(object? obj)
            {
                return (obj is IsOsSyntax orientation) && orientation.Argument == Argument;
            }
        }

        public class MinWidthSyntax : ISyntax
        {
            public double Argument { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is MinWidthSyntax minwidth) && minwidth.Argument == Argument;
            }
        }

        public class MinHeightSyntax : ISyntax
        {
            public double Argument { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is MinHeightSyntax minwidth) && minwidth.Argument == Argument;
            }
        }

        public class MaxWidthSyntax : ISyntax
        {
            public double Argument { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is MaxWidthSyntax maxwidth) && maxwidth.Argument == Argument;
            }
        }

        public class MaxHeightSyntax : ISyntax
        {
            public double Argument { get; set; }

            public override bool Equals(object? obj)
            {
                return (obj is MaxHeightSyntax maxHeight) && maxHeight.Argument == Argument;
            }
        }
    }
}
