// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Data.Core;
using Avalonia.Utilities;

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the 
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

namespace Avalonia.Markup.Parsers
{
    internal static class SelectorGrammar
    {
        private enum State
        {
            Start,
            Middle,
            Colon,
            Class,
            Name,
            CanHaveType,
            Traversal,
            TypeName,
            Property,
            Template,
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
                ISyntax syntax = null;
                switch (state)
                {
                    case State.Start:
                        state = ParseStart(ref r);
                        break;
                    case State.Middle:
                        (state, syntax) = ParseMiddle(ref r, end);
                        break;
                    case State.CanHaveType:
                        state = ParseCanHaveType(ref r);
                        break;
                    case State.Colon:
                        (state, syntax) = ParseColon(ref r);
                        break;
                    case State.Class:
                        (state, syntax) = ParseClass(ref r);
                        break;
                    case State.Traversal:
                        (state, syntax) = ParseTraversal(ref r);
                        break;
                    case State.TypeName:
                        (state, syntax) = ParseTypeName(ref r);
                        break;
                    case State.Property:
                        (state, syntax) = ParseProperty(ref r);
                        break;
                    case State.Template:
                        (state, syntax) = ParseTemplate(ref r);
                        break;
                    case State.Name:
                        (state, syntax) = ParseName(ref r);
                        break;
                }
                if (syntax != null)
                {
                    selector.Add(syntax);
                }
            }

            if (state != State.Start && state != State.Middle && state != State.End && state != State.CanHaveType)
            {
                throw new ExpressionParseException(r.Position, "Unexpected end of selector");
            }

            return selector;
        }

        private static State ParseStart(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.End)
            {
                return State.End;
            }

            if (r.TakeIf(':'))
            {
                return State.Colon;
            }
            else if (r.TakeIf('.'))
            {
                return State.Class;
            }
            else if (r.TakeIf('#'))
            {
                return State.Name;
            }
            return State.TypeName;
        }

        private static (State, ISyntax) ParseMiddle(ref CharacterReader r, char? end)
        {
            if (r.TakeIf(':'))
            {
                return (State.Colon, null);
            }
            else if (r.TakeIf('.'))
            {
                return (State.Class, null);
            }
            else if (r.TakeIf(char.IsWhiteSpace) || r.Peek == '>')
            {
                return (State.Traversal, null);
            }
            else if (r.TakeIf('/'))
            {
                return (State.Template, null);
            }
            else if (r.TakeIf('#'))
            {
                return (State.Name, null);
            }
            else if (r.TakeIf(','))
            {
                return (State.Start, new CommaSyntax());
            }
            else if (end.HasValue && !r.End && r.Peek == end.Value)
            {
                return (State.End, null);
            }
            return (State.TypeName, null);
        }

        private static State ParseCanHaveType(ref CharacterReader r)
        {
            if (r.TakeIf('['))
            {
                return State.Property;
            }
            return State.Middle;
        }

        private static (State, ISyntax) ParseColon(ref CharacterReader r)
        {
            var identifier = r.ParseIdentifier();

            if (identifier.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Expected class name or is selector after ':'.");
            }

            const string IsKeyword = "is";
            const string NotKeyword = "not";

            if (identifier.SequenceEqual(IsKeyword.AsSpan()) && r.TakeIf('('))
            {
                var syntax = ParseType(ref r, new IsSyntax());
                Expect(ref r, ')');

                return (State.CanHaveType, syntax);
            }
            if (identifier.SequenceEqual(NotKeyword.AsSpan()) && r.TakeIf('('))
            {
                var argument = Parse(ref r, ')');
                Expect(ref r, ')');

                var syntax = new NotSyntax { Argument = argument };
                return (State.Middle, syntax);
            }
            else
            {
                return (
                    State.CanHaveType,
                    new ClassSyntax
                    {
                        Class = ":" + identifier.ToString()
                    });
            }
        }

        private static (State, ISyntax) ParseTraversal(ref CharacterReader r)
        {
            r.SkipWhitespace();
            if (r.TakeIf('>'))
            {
                r.SkipWhitespace();
                return (State.Middle, new ChildSyntax());
            }
            else if (r.TakeIf('/'))
            {
                return (State.Template, null);
            }
            else if (!r.End)
            {
                return (State.Middle, new DescendantSyntax());
            }
            else
            {
                return (State.End, null);
            }
        }

        private static (State, ISyntax) ParseClass(ref CharacterReader r)
        {
            var @class = r.ParseIdentifier();
            if (@class.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a class name after '.'.");
            }

            return (State.CanHaveType, new ClassSyntax { Class = @class.ToString() });
        }

        private static (State, ISyntax) ParseTemplate(ref CharacterReader r)
        {
            var template = r.ParseIdentifier();
            const string TemplateKeyword = "template";
            if (!template.SequenceEqual(TemplateKeyword.AsSpan()))
            {
                throw new ExpressionParseException(r.Position, $"Expected 'template', got '{template.ToString()}'");
            }
            else if (!r.TakeIf('/'))
            {
                throw new ExpressionParseException(r.Position, "Expected '/'");
            }
            return (State.Start, new TemplateSyntax());
        }

        private static (State, ISyntax) ParseName(ref CharacterReader r)
        {
            var name = r.ParseIdentifier();
            if (name.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a name after '#'.");
            }
            return (State.CanHaveType, new NameSyntax { Name = name.ToString() });
        }

        private static (State, ISyntax) ParseTypeName(ref CharacterReader r)
        {
            return (State.CanHaveType, ParseType(ref r, new OfTypeSyntax()));
        }

        private static (State, ISyntax) ParseProperty(ref CharacterReader r)
        {
            var property = r.ParseIdentifier();

            if (!r.TakeIf('='))
            {
                throw new ExpressionParseException(r.Position, $"Expected '=', got '{r.Peek}'");
            }

            var value = r.TakeUntil(']');

            r.Take();

            return (State.CanHaveType, new PropertySyntax { Property = property.ToString(), Value = value.ToString() });
        }

        private static TSyntax ParseType<TSyntax>(ref CharacterReader r, TSyntax syntax)
            where TSyntax : ITypeSyntax
        {
            ReadOnlySpan<char> ns = default;
            ReadOnlySpan<char> type;
            var namespaceOrTypeName = r.ParseIdentifier();

            if (namespaceOrTypeName.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected an identifier, got '{r.Peek}");
            }

            if (!r.End && r.TakeIf('|'))
            {
                ns = namespaceOrTypeName;
                if (r.End)
                {
                    throw new ExpressionParseException(r.Position, $"Unexpected end of selector.");
                }
                type = r.ParseIdentifier();
            }
            else
            {
                type = namespaceOrTypeName;
            }

            syntax.Xmlns = ns.ToString();
            syntax.TypeName = type.ToString();
            return syntax;
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

        public interface ISyntax
        {
        }

        public interface ITypeSyntax
        {
            string TypeName { get; set; }

            string Xmlns { get; set; }
        }

        public class OfTypeSyntax : ISyntax, ITypeSyntax
        {
            public string TypeName { get; set; }

            public string Xmlns { get; set; } = string.Empty;

            public override bool Equals(object obj)
            {
                var other = obj as OfTypeSyntax;
                return other != null && other.TypeName == TypeName && other.Xmlns == Xmlns;
            }
        }

        public class IsSyntax : ISyntax, ITypeSyntax
        {
            public string TypeName { get; set; }

            public string Xmlns { get; set; } = string.Empty;

            public override bool Equals(object obj)
            {
                var other = obj as IsSyntax;
                return other != null && other.TypeName == TypeName && other.Xmlns == Xmlns;
            }
        }

        public class ClassSyntax : ISyntax
        {
            public string Class { get; set; }

            public override bool Equals(object obj)
            {
                return obj is ClassSyntax && ((ClassSyntax)obj).Class == Class;
            }
        }

        public class NameSyntax : ISyntax
        {
            public string Name { get; set; }

            public override bool Equals(object obj)
            {
                return obj is NameSyntax && ((NameSyntax)obj).Name == Name;
            }
        }

        public class PropertySyntax : ISyntax
        {
            public string Property { get; set; }

            public string Value { get; set; }

            public override bool Equals(object obj)
            {
                return obj is PropertySyntax && 
                    ((PropertySyntax)obj).Property == Property && 
                    ((PropertySyntax)obj).Value == Value;
            }
        }

        public class ChildSyntax : ISyntax
        {
            public override bool Equals(object obj)
            {
                return obj is ChildSyntax;
            }
        }

        public class DescendantSyntax : ISyntax
        {
            public override bool Equals(object obj)
            {
                return obj is DescendantSyntax;
            }
        }

        public class TemplateSyntax : ISyntax
        {
            public override bool Equals(object obj)
            {
                return obj is TemplateSyntax;
            }
        }

        public class NotSyntax : ISyntax
        {
            public IEnumerable<ISyntax> Argument { get; set; }

            public override bool Equals(object obj)
            {
                return (obj is NotSyntax not) && Argument.SequenceEqual(not.Argument);
            }
        }

        public class CommaSyntax : ISyntax
        {
            public override bool Equals(object obj)
            {
                return obj is CommaSyntax or;
            }
        }
    }
}
