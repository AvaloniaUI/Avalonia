using System;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;
using Avalonia.Markup.Xaml.Parsers;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Markup.Xaml.UnitTests.Parsers
{
    public class PropertyParserTests
    {
        [Fact]
        public void Parses_Name()
        {
            var reader = new CharacterReader("Foo".AsSpan());
            var (ns, owner, name) = PropertyParser.Parse(reader);

            Assert.Null(ns);
            Assert.Null(owner);
            Assert.Equal("Foo", name);
        }

        [Fact]
        public void Parses_Owner_And_Name()
        {
            var reader = new CharacterReader("Foo.Bar".AsSpan());
            var (ns, owner, name) = PropertyParser.Parse(reader);

            Assert.Null(ns);
            Assert.Equal("Foo", owner);
            Assert.Equal("Bar", name);
        }

        [Fact]
        public void Parses_Namespace_Owner_And_Name()
        {
            var reader = new CharacterReader("foo:Bar.Baz".AsSpan());
            var (ns, owner, name) = PropertyParser.Parse(reader);

            Assert.Equal("foo", ns);
            Assert.Equal("Bar", owner);
            Assert.Equal("Baz", name);
        }

        [Fact]
        public void Parses_Owner_And_Name_With_Parentheses()
        {
            var reader = new CharacterReader("(Foo.Bar)".AsSpan());
            var (ns, owner, name) = PropertyParser.Parse(reader);

            Assert.Null(ns);
            Assert.Equal("Foo", owner);
            Assert.Equal("Bar", name);
        }

        [Fact]
        public void Parses_Namespace_Owner_And_Name_With_Parentheses()
        {
            var reader = new CharacterReader("(foo:Bar.Baz)".AsSpan());
            var (ns, owner, name) = PropertyParser.Parse(reader);

            Assert.Equal("foo", ns);
            Assert.Equal("Bar", owner);
            Assert.Equal("Baz", name);
        }

        [Fact]
        public void Fails_With_Empty_String()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader(ReadOnlySpan<char>.Empty)));
            Assert.Equal(0, ex.Column);
            Assert.Equal("Expected property name.", ex.Message);
        }

        [Fact]
        public void Fails_With_Only_Whitespace()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("  ".AsSpan())));
            Assert.Equal(0, ex.Column);
            Assert.Equal("Unexpected ' '.", ex.Message);
        }

        [Fact]
        public void Fails_With_Leading_Whitespace()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader(" Foo".AsSpan())));
            Assert.Equal(0, ex.Column);
            Assert.Equal("Unexpected ' '.", ex.Message);
        }

        [Fact]
        public void Fails_With_Trailing_Whitespace()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo ".AsSpan())));
            Assert.Equal(3, ex.Column);
            Assert.Equal("Unexpected ' '.", ex.Message);
        }

        [Fact]
        public void Fails_With_Invalid_Property_Name()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("123".AsSpan())));
            Assert.Equal(0, ex.Column);
            Assert.Equal("Unexpected '1'.", ex.Message);
        }

        [Fact]
        public void Fails_With_Trailing_Junk()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo%".AsSpan())));
            Assert.Equal(3, ex.Column);
            Assert.Equal("Unexpected '%'.", ex.Message);
        }

        [Fact]
        public void Fails_With_Invalid_Property_Name_After_Owner()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo.123".AsSpan())));
            Assert.Equal(4, ex.Column);
            Assert.Equal("Unexpected '1'.", ex.Message);
        }

        [Fact]
        public void Fails_With_Whitespace_Between_Owner_And_Name()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo. Bar".AsSpan())));
            Assert.Equal(4, ex.Column);
            Assert.Equal("Unexpected ' '.", ex.Message);
        }

        [Fact]
        public void Fails_With_Too_Many_Segments()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo.Bar.Baz".AsSpan())));
            Assert.Equal(8, ex.Column);
            Assert.Equal("Unexpected '.'.", ex.Message);
        }

        [Fact]
        public void Fails_With_Too_Many_Namespaces()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("foo:bar:Baz".AsSpan())));
            Assert.Equal(8, ex.Column);
            Assert.Equal("Unexpected ':'.", ex.Message);
        }

        [Fact]
        public void Fails_With_Parens_But_No_Owner()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("(Foo)".AsSpan())));
            Assert.Equal(1, ex.Column);
            Assert.Equal("Expected property owner.", ex.Message);
        }

        [Fact]
        public void Fails_With_Parens_And_Namespace_But_No_Owner()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("(foo:Bar)".AsSpan())));
            Assert.Equal(1, ex.Column);
            Assert.Equal("Expected property owner.", ex.Message);
        }

        [Fact]
        public void Fails_With_Missing_Close_Parens()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("(Foo.Bar".AsSpan())));
            Assert.Equal(8, ex.Column);
            Assert.Equal("Expected ')'.", ex.Message);
        }

        [Fact]
        public void Fails_With_Unexpected_Close_Parens()
        {
            var ex = Assert.Throws<ExpressionParseException>(() => PropertyParser.Parse(new CharacterReader("Foo.Bar)".AsSpan())));
            Assert.Equal(7, ex.Column);
            Assert.Equal("Unexpected ')'.", ex.Message);
        }
    }
}
