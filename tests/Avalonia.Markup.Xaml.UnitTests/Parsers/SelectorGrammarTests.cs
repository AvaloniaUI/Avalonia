// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Markup.Xaml.Parsers;
using Sprache;
using Xunit;

namespace Avalonia.Xaml.Base.UnitTest.Parsers
{
    public class SelectorGrammarTests
    {
        [Fact]
        public void OfType()
        {
            var result = SelectorGrammar.Selector.Parse("Button").ToList();

            Assert.Equal(
                new[] { new SelectorGrammar.OfTypeSyntax { TypeName = "Button", Xmlns = null } },
                result);
        }

        [Fact]
        public void NamespacedOfType()
        {
            var result = SelectorGrammar.Selector.Parse("x|Button").ToList();

            Assert.Equal(
                new[] { new SelectorGrammar.OfTypeSyntax { TypeName = "Button", Xmlns = "x" } },
                result);
        }

        [Fact]
        public void Name()
        {
            var result = SelectorGrammar.Selector.Parse("#foo").ToList();

            Assert.Equal(
                new[] { new SelectorGrammar.NameSyntax { Name = "foo" }, },
                result);
        }

        [Fact]
        public void OfType_Name()
        {
            var result = SelectorGrammar.Selector.Parse("Button#foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.NameSyntax { Name = "foo" },
                },
                result);
        }

        [Fact]
        public void Is()
        {
            var result = SelectorGrammar.Selector.Parse(":is(Button)").ToList();

            Assert.Equal(
                new[] { new SelectorGrammar.IsSyntax { TypeName = "Button", Xmlns = null } },
                result);
        }

        [Fact]
        public void Is_Name()
        {
            var result = SelectorGrammar.Selector.Parse(":is(Button)#foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.IsSyntax { TypeName = "Button" },
                    new SelectorGrammar.NameSyntax { Name = "foo" },
                },
                result);
        }

        [Fact]
        public void NamespacedIs_Name()
        {
            var result = SelectorGrammar.Selector.Parse(":is(x|Button)#foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.IsSyntax { TypeName = "Button", Xmlns = "x" },
                    new SelectorGrammar.NameSyntax { Name = "foo" },
                },
                result);
        }

        [Fact]
        public void Class()
        {
            var result = SelectorGrammar.Selector.Parse(".foo").ToList();

            Assert.Equal(
                new[] { new SelectorGrammar.ClassSyntax { Class = "foo" } },
                result);
        }

        [Fact]
        public void Pseudoclass()
        {
            var result = SelectorGrammar.Selector.Parse(":foo").ToList();

            Assert.Equal(
                new[] { new SelectorGrammar.ClassSyntax { Class = ":foo" } },
                result);
        }

        [Fact]
        public void OfType_Class()
        {
            var result = SelectorGrammar.Selector.Parse("Button.foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[] 
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.ClassSyntax { Class = "foo" },
                },
                result);
        }

        [Fact]
        public void OfType_Child_Class()
        {
            var result = SelectorGrammar.Selector.Parse("Button > .foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.ChildSyntax { },
                    new SelectorGrammar.ClassSyntax { Class = "foo" },
                },
                result);
        }

        [Fact]
        public void OfType_Child_Class_No_Spaces()
        {
            var result = SelectorGrammar.Selector.Parse("Button>.foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.ChildSyntax { },
                    new SelectorGrammar.ClassSyntax { Class = "foo" },
                },
                result);
        }

        [Fact]
        public void OfType_Descendant_Class()
        {
            var result = SelectorGrammar.Selector.Parse("Button .foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.DescendantSyntax { },
                    new SelectorGrammar.ClassSyntax { Class = "foo" },
                },
                result);
        }

        [Fact]
        public void OfType_Template_Class()
        {
            var result = SelectorGrammar.Selector.Parse("Button /template/ .foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.TemplateSyntax { },
                    new SelectorGrammar.ClassSyntax { Class = "foo" },
                },
                result);
        }

        [Fact]
        public void OfType_Property()
        {
            var result = SelectorGrammar.Selector.Parse("Button[Foo=bar]").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.PropertySyntax { Property = "Foo", Value = "bar" },
                },
                result);
        }

        [Fact]
        public void Namespace_Alone_Fails()
        {
            Assert.Throws<ParseException>(() => SelectorGrammar.Selector.Parse("ns|").ToList());
        }

        [Fact]
        public void Dot_Alone_Fails()
        {
            Assert.Throws<ParseException>(() => SelectorGrammar.Selector.Parse(". dot").ToList());
        }

        [Fact]
        public void Invalid_Identifier_Fails()
        {
            Assert.Throws<ParseException>(() => SelectorGrammar.Selector.Parse("%foo").ToList());
        }

        [Fact]
        public void Invalid_Class_Fails()
        {
            Assert.Throws<ParseException>(() => SelectorGrammar.Selector.Parse(".%foo").ToList());
        }
    }
}
