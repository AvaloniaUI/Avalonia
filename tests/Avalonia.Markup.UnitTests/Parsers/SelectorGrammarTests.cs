using System.Linq;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Xunit;

namespace Avalonia.Markup.UnitTests.Parsers
{
    public class SelectorGrammarTests
    {
        [Fact]
        public void OfType()
        {
            var result = SelectorGrammar.Parse("Button");

            Assert.Equal(
                new[] { new SelectorGrammar.OfTypeSyntax { TypeName = "Button", Xmlns = "" } },
                result);
        }

        [Fact]
        public void NamespacedOfType()
        {
            var result = SelectorGrammar.Parse("x|Button");

            Assert.Equal(
                new[] { new SelectorGrammar.OfTypeSyntax { TypeName = "Button", Xmlns = "x" } },
                result);
        }

        [Fact]
        public void Name()
        {
            var result = SelectorGrammar.Parse("#foo");

            Assert.Equal(
                new[] { new SelectorGrammar.NameSyntax { Name = "foo" }, },
                result);
        }

        [Fact]
        public void OfType_Name()
        {
            var result = SelectorGrammar.Parse("Button#foo");

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
            var result = SelectorGrammar.Parse(":is(Button)");

            Assert.Equal(
                new[] { new SelectorGrammar.IsSyntax { TypeName = "Button", Xmlns = "" } },
                result);
        }

        [Fact]
        public void Is_Name()
        {
            var result = SelectorGrammar.Parse(":is(Button)#foo");

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
            var result = SelectorGrammar.Parse(":is(x|Button)#foo");

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
            var result = SelectorGrammar.Parse(".foo");

            Assert.Equal(
                new[] { new SelectorGrammar.ClassSyntax { Class = "foo" } },
                result);
        }

        [Fact]
        public void Pseudoclass()
        {
            var result = SelectorGrammar.Parse(":foo");

            Assert.Equal(
                new[] { new SelectorGrammar.ClassSyntax { Class = ":foo" } },
                result);
        }

        [Fact]
        public void OfType_Class()
        {
            var result = SelectorGrammar.Parse("Button.foo");

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
            var result = SelectorGrammar.Parse("Button > .foo");

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
            var result = SelectorGrammar.Parse("Button>.foo");

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
            var result = SelectorGrammar.Parse("Button .foo");

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
            var result = SelectorGrammar.Parse("Button /template/ .foo");

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
            var result = SelectorGrammar.Parse("Button[Foo=bar]");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.PropertySyntax { Property = "Foo", Value = "bar" },
                },
                result);
        }

        [Fact]
        public void Not_OfType()
        {
            var result = SelectorGrammar.Parse(":not(Button)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.NotSyntax
                    {
                        Argument = new SelectorGrammar.ISyntax[]
                        {
                            new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                        },
                    }
                },
                result);
        }

        [Fact]
        public void OfType_Not_Class()
        {
            var result = SelectorGrammar.Parse("Button:not(.foo)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.NotSyntax
                    {
                        Argument = new SelectorGrammar.ISyntax[]
                        {
                            new SelectorGrammar.ClassSyntax { Class = "foo" },
                        },
                    }
                },
                result);
        }

        [Fact]
        public void OfType_NthChild()
        {
            var result = SelectorGrammar.Parse("Button:nth-child(2n+1)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.NthChildSyntax()
                    {
                        Step = 2,
                        Offset = 1
                    }
                },
                result);
        }

        [Fact]
        public void OfType_NthChild_Without_Offset()
        {
            var result = SelectorGrammar.Parse("Button:nth-child(2147483647n)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.NthChildSyntax()
                    {
                        Step = int.MaxValue,
                        Offset = 0
                    }
                },
                result);
        }

        [Fact]
        public void OfType_NthLastChild()
        {
            var result = SelectorGrammar.Parse("Button:nth-last-child(2n+1)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.NthLastChildSyntax()
                    {
                        Step = 2,
                        Offset = 1
                    }
                },
                result);
        }

        [Fact]
        public void OfType_NthChild_Odd()
        {
            var result = SelectorGrammar.Parse("Button:nth-child(odd)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.NthChildSyntax()
                    {
                        Step = 2,
                        Offset = 1
                    }
                },
                result);
        }

        [Fact]
        public void OfType_NthChild_Even()
        {
            var result = SelectorGrammar.Parse("Button:nth-child(even)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                    new SelectorGrammar.NthChildSyntax()
                    {
                        Step = 2,
                        Offset = 0
                    }
                },
                result);
        }

        [Fact]
        public void Is_Descendent_Not_OfType_Class()
        {
            var result = SelectorGrammar.Parse(":is(Control) :not(Button.foo)");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.IsSyntax { TypeName = "Control" },
                    new SelectorGrammar.DescendantSyntax { },
                    new SelectorGrammar.NotSyntax
                    {
                        Argument = new SelectorGrammar.ISyntax[]
                        {
                            new SelectorGrammar.OfTypeSyntax { TypeName = "Button" },
                            new SelectorGrammar.ClassSyntax { Class = "foo" },
                        },
                    }
                },
                result);
        }

        [Fact]
        public void OfType_Comma_Is_Class()
        {
            var result = SelectorGrammar.Parse("TextBlock, :is(Button).foo");

            Assert.Equal(
                new SelectorGrammar.ISyntax[]
                {
                    new SelectorGrammar.OfTypeSyntax { TypeName = "TextBlock" },
                    new SelectorGrammar.CommaSyntax(),
                    new SelectorGrammar.IsSyntax { TypeName = "Button" },
                    new SelectorGrammar.ClassSyntax { Class = "foo" },
                },
                result);
        }

        [Fact]
        public void Namespace_Alone_Fails()
        {
            Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse("ns|"));
        }

        [Fact]
        public void Dot_Alone_Fails()
        {
            Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(". dot"));
        }

        [Fact]
        public void Invalid_Identifier_Fails()
        {
            Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse("%foo"));
        }

        [Fact]
        public void Invalid_Class_Fails()
        {
            Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(".%foo"));
        }

        [Fact]
        public void Not_Without_Argument_Fails()
        {
            Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(":not()"));
        }

        [Fact]
        public void Not_Without_Closing_Parenthesis_Fails()
        {
            Assert.Throws<ExpressionParseException>(() => SelectorGrammar.Parse(":not(Button"));
        }
    }
}
