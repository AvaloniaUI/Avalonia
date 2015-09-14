// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Markup.Xaml.Parsers;
using Sprache;
using Xunit;

namespace Perspex.Xaml.Base.UnitTest.Parsers
{
    public class SelectorGrammarTests
    {
        [Fact]
        public void OfType()
        {
            var result = SelectorGrammar.Selector.Parse("Button").ToList();

            Assert.Equal(
                new[] { new SelectorGrammar.OfTypeSyntax { TypeName = "Button" } },
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
            var result = SelectorGrammar.Selector.Parse("Button < .foo").ToList();

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
        public void OfType_Descendent_Class()
        {
            var result = SelectorGrammar.Selector.Parse("Button .foo").ToList();

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.DescendentSyntax { },
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
    }
}
