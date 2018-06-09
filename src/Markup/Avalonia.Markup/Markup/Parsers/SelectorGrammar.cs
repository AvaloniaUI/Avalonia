// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Core;

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the 
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

namespace Avalonia.Markup.Parsers
{
    internal class SelectorGrammar
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
            var r = new Reader(s);
            var state = State.Start;
            var selector = new List<ISyntax>();
            while (!r.End && state != State.End)
            {
                ISyntax syntax = null;
                switch (state)
                {
                    case State.Start:
                        (state, syntax) = ParseStart(r);
                        break;
                    case State.Middle:
                        (state, syntax) = ParseMiddle(r);
                        break;
                    case State.Colon:
                        (state, syntax) = ParseColon(r);
                        break;
                    case State.Class:
                        (state, syntax) = ParseClass(r);
                        break;
                    case State.Traversal:
                        (state, syntax) = ParseTraversal(r);
                        break;
                    case State.TypeName:
                        (state, syntax) = ParseTypeName(r);
                        break;
                    case State.CanHaveType:
                        (state, syntax) = ParseCanHaveType(r);
                        break;
                    case State.Property:
                        (state, syntax) = ParseProperty(r);
                        break;
                    case State.Template:
                        (state, syntax) = ParseTemplate(r);
                        break;
                    case State.Name:
                        (state, syntax) = ParseName(r);
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

        private static (State, ISyntax) ParseStart(Reader r)
        {
            r.SkipWhitespace();
            if (r.TakeIf(':'))
            {
                return (State.Colon, null);
            }
            else if (r.TakeIf('.'))
            {
                return (State.Class, null);
            }
            else if (r.TakeIf('#'))
            {
                return (State.Name, null);
            }
            return (State.TypeName, null);
        }

        private static (State, ISyntax) ParseMiddle(Reader r)
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
            return (State.TypeName, null);
        }

        private static (State, ISyntax) ParseCanHaveType(Reader r)
        {
            if (r.TakeIf('['))
            {
                return (State.Property, null);
            }
            return (State.Middle, null);
        }

        private static (State, ISyntax) ParseColon(Reader r)
        {
            var identifier = r.ParseIdentifier();

            if (identifier.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, "Expected class name or is selector after ':'.");
            }

            if (identifier.SequenceEqual("is".AsSpan()) && r.TakeIf('('))
            {
                var syntax = ParseType<IsSyntax>(r);
                if (r.End || !r.TakeIf(')'))
                {
                    throw new ExpressionParseException(r.Position, $"Expected ')', got {r.Peek}");
                }

                return (State.CanHaveType, syntax);
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

        private static (State, ISyntax) ParseTraversal(Reader r)
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

        private static (State, ISyntax) ParseClass(Reader r)
        {
            var @class = r.ParseIdentifier();
            if (@class.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a class name after '.'.");
            }

            return (State.CanHaveType, new ClassSyntax { Class = @class.ToString() });
        }

        private static (State, ISyntax) ParseTemplate(Reader r)
        {
            var template = r.ParseIdentifier();
            if (!template.SequenceEqual(nameof(template).AsSpan()))
            {
                throw new ExpressionParseException(r.Position, $"Expected 'template', got '{template.ToString()}'");
            }
            else if (!r.TakeIf('/'))
            {
                throw new ExpressionParseException(r.Position, "Expected '/'");
            }
            return (State.Start, new TemplateSyntax());
        }

        private static (State, ISyntax) ParseName(Reader r)
        {
            var name = r.ParseIdentifier();
            if (name.IsEmpty)
            {
                throw new ExpressionParseException(r.Position, $"Expected a name after '#'.");
            }
            return (State.CanHaveType, new NameSyntax { Name = name.ToString() });
        }

        private static (State, ISyntax) ParseTypeName(Reader r)
        {
            return (State.CanHaveType, ParseType<OfTypeSyntax>(r));
        }

        private static (State, ISyntax) ParseProperty(Reader r)
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

        private static TSyntax ParseType<TSyntax>(Reader r)
            where TSyntax : ITypeSyntax, new()
        {
            ReadOnlySpan<char> ns = null;
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
            return new TSyntax
            {
                Xmlns = ns.ToString(),
                TypeName = type.ToString()
            };
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
    }
}
