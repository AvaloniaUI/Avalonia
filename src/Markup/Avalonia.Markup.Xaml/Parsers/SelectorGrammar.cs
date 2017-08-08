// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using Sprache;

// Don't need to override GetHashCode as the ISyntax objects will not be stored in a hash; the 
// only reason they have overridden Equals methods is for unit testing.
#pragma warning disable 659

namespace Avalonia.Markup.Xaml.Parsers
{
    internal class SelectorGrammar
    {
        public static readonly Parser<char> CombiningCharacter = Parse.Char(
            c =>
            {
                var cat = CharUnicodeInfo.GetUnicodeCategory(c);
                return cat == UnicodeCategory.NonSpacingMark ||
                       cat == UnicodeCategory.SpacingCombiningMark;
            },
            "Connecting Character");

        public static readonly Parser<char> ConnectingCharacter = Parse.Char(
            c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.ConnectorPunctuation,
            "Connecting Character");

        public static readonly Parser<char> FormattingCharacter = Parse.Char(
            c => CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format,
            "Connecting Character");

        public static readonly Parser<char> IdentifierStart = Parse.Letter.Or(Parse.Char('_'));

        public static readonly Parser<char> IdentifierChar = Parse
            .LetterOrDigit
            .Or(ConnectingCharacter)
            .Or(CombiningCharacter)
            .Or(FormattingCharacter);

        public static readonly Parser<string> Identifier =
            from start in IdentifierStart.Once().Text()
            from @char in IdentifierChar.Many().Text()
            select start + @char;

        public static readonly Parser<string> Namespace =
            from ns in Parse.Letter.Many().Text()
            from bar in Parse.Char('|')
            select ns;

        public static readonly Parser<OfTypeSyntax> OfType =
            from ns in Namespace.Optional()
            from identifier in Identifier
            select new OfTypeSyntax
            {
                TypeName = identifier,
                Xmlns = ns.GetOrDefault(),
            };

        public static readonly Parser<NameSyntax> Name =
            from hash in Parse.Char('#')
            from identifier in Identifier
            select new NameSyntax { Name = identifier };

        public static readonly Parser<char> ClassStart = Parse.Char('_').Or(Parse.Letter);

        public static readonly Parser<char> ClassChar = ClassStart.Or(Parse.Numeric);

        public static readonly Parser<string> ClassIdentifier =
            from start in ClassStart.Once().Text()
            from @char in ClassChar.Many().Text()
            select start + @char;

        public static readonly Parser<ClassSyntax> StandardClass =
            from dot in Parse.Char('.').Once()
            from identifier in ClassIdentifier
            select new ClassSyntax { Class = identifier };

        public static readonly Parser<ClassSyntax> Pseduoclass =
            from colon in Parse.Char(':').Once()
            from identifier in ClassIdentifier
            select new ClassSyntax { Class = ':' + identifier };

        public static readonly Parser<ClassSyntax> Class = StandardClass.Or(Pseduoclass);

        public static readonly Parser<PropertySyntax> Property =
            from open in Parse.Char('[').Once()
            from identifier in Identifier
            from eq in Parse.Char('=').Once()
            from value in Parse.CharExcept(']').Many().Text()
            from close in Parse.Char(']').Once()
            select new PropertySyntax { Property = identifier, Value = value };

        public static readonly Parser<ChildSyntax> Child = Parse.Char('>').Token().Return(new ChildSyntax());

        public static readonly Parser<DescendantSyntax> Descendant =
            from child in Parse.WhiteSpace.Many()
            select new DescendantSyntax();

        public static readonly Parser<TemplateSyntax> Template =
            from template in Parse.String("/template/").Token()
            select new TemplateSyntax();

        public static readonly Parser<IsSyntax> Is =
            from function in Parse.String(":is(")
            from type in OfType
            from close in Parse.Char(')')
            select new IsSyntax { TypeName = type.TypeName, Xmlns = type.Xmlns };

        public static readonly Parser<ISyntax> SingleSelector =
            OfType
            .Or<ISyntax>(Is)
            .Or<ISyntax>(Name)
            .Or<ISyntax>(Class)
            .Or<ISyntax>(Property)
            .Or<ISyntax>(Child)
            .Or<ISyntax>(Template)
            .Or<ISyntax>(Descendant);

        public static readonly Parser<IEnumerable<ISyntax>> Selector = SingleSelector.Many().End();
        
        public interface ISyntax
        {
        }

        public class OfTypeSyntax : ISyntax
        {
            public string TypeName { get; set; }

            public string Xmlns { get; set; }

            public override bool Equals(object obj)
            {
                var other = obj as OfTypeSyntax;
                return other != null && other.TypeName == TypeName && other.Xmlns == Xmlns;
            }
        }

        public class IsSyntax : ISyntax
        {
            public string TypeName { get; set; }

            public string Xmlns { get; set; }

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
