using System.Collections.Generic;
using Avalonia.Data.Core.Parsers;
using Avalonia.UnitTests;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Base.UnitTests.Data.Core.Parsers
{
    public partial class BindingExpressionGrammarTests : ScopedTestBase
    {
        [Fact]
        public void Should_Parse_Single_Property()
        {
            var result = Parse("Foo");
            var node = Assert.Single(result);

            AssertIsProperty(node, "Foo");
        }

        [Fact]
        public void Should_Parse_Underscored_Property()
        {
            var result = Parse("_Foo");
            var node = Assert.Single(result);

            AssertIsProperty(node, "_Foo");
        }

        [Fact]
        public void Should_Parse_Property_With_Digits()
        {
            var result = Parse("F0o");
            var node = Assert.Single(result);

            AssertIsProperty(node, "F0o");
        }

        [Fact]
        public void Should_Parse_Dot()
        {
            var result = Parse(".");
            var node = Assert.Single(result);

            Assert.IsType<BindingExpressionGrammar.EmptyExpressionNode>(node);
        }

        [Fact]
        public void Should_Parse_Single_Attached_Property()
        {
            var result = Parse("(Foo.Bar)");
            var node = Assert.Single(result);

            AssertIsAttachedProperty(node, "Foo", "Bar");
        }

        [Fact]
        public void Should_Parse_Property_Chain()
        {
            var result = Parse("Foo.Bar.Baz");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar");
            AssertIsProperty(result[2], "Baz");
        }

        [Fact]
        public void Should_Parse_Property_Chain_With_Attached_Property_1()
        {
            var result = Parse("(Foo.Bar).Baz");

            Assert.Equal(2, result.Count);
            AssertIsAttachedProperty(result[0], "Foo", "Bar");
            AssertIsProperty(result[1], "Baz");
        }

        [Fact]
        public void Should_Parse_Property_Chain_With_Attached_Property_2()
        {
            var result = Parse("Foo.(Bar.Baz)");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsAttachedProperty(result[1], "Bar", "Baz");
        }

        [Fact]
        public void Should_Parse_Property_Chain_With_Attached_Property_3()
        {
            var result = Parse("Foo.(Bar.Baz).Last");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsAttachedProperty(result[1], "Bar", "Baz");
            AssertIsProperty(result[2], "Last");
        }

        [Fact]
        public void Should_Parse_Null_Conditional_In_Property_Chain_1()
        {
            var result = Parse("Foo?.Bar.Baz");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar", acceptsNull: true);
            AssertIsProperty(result[2], "Baz");
        }

        [Fact]
        public void Should_Parse_Null_Conditional_In_Property_Chain_2()
        {
            var result = Parse("Foo.Bar?.Baz");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar");
            AssertIsProperty(result[2], "Baz", acceptsNull: true);
        }

        [Fact]
        public void Should_Parse_Null_Conditional_In_Property_Chain_3()
        {
            var result = Parse("Foo?.(Bar.Baz)");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsAttachedProperty(result[1], "Bar", "Baz", acceptsNull: true);
        }

        [Fact]
        public void Should_Parse_Negated_Property_Chain()
        {
            var result = Parse("!Foo.Bar.Baz");

            Assert.Equal(4, result.Count);
            Assert.IsType<BindingExpressionGrammar.NotNode>(result[0]);
            AssertIsProperty(result[1], "Foo");
            AssertIsProperty(result[2], "Bar");
            AssertIsProperty(result[3], "Baz");
        }

        [Fact]
        public void Should_Parse_Double_Negated_Property_Chain()
        {
            var result = Parse("!!Foo.Bar.Baz");

            Assert.Equal(5, result.Count);
            Assert.IsType<BindingExpressionGrammar.NotNode>(result[0]);
            Assert.IsType<BindingExpressionGrammar.NotNode>(result[1]);
            AssertIsProperty(result[2], "Foo");
            AssertIsProperty(result[3], "Bar");
            AssertIsProperty(result[4], "Baz");
        }

        [Fact]
        public void Should_Parse_Indexed_Property()
        {
            var result = Parse("Foo[15]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "15");
        }

        [Fact]
        public void Should_Parse_Indexed_Property_StringIndex()
        {
            var result = Parse("Foo[Key]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "Key");
        }

        [Fact]
        public void Should_Parse_Multiple_Indexed_Property()
        {
            var result = Parse("Foo[15,6]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "15", "6");
        }

        [Fact]
        public void Should_Parse_Multiple_Indexed_Property_With_Space()
        {
            var result = Parse("Foo[5, 16]");

            Assert.Equal(2, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "5", "16");
        }

        [Fact]
        public void Should_Parse_Consecutive_Indexers()
        {
            var result = Parse("Foo[15][16]");

            Assert.Equal(3, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsIndexer(result[1], "15");
            AssertIsIndexer(result[2], "16");
        }

        [Fact]
        public void Should_Parse_Indexed_Property_In_Chain()
        {
            var result = Parse("Foo.Bar[5, 6].Baz");

            Assert.Equal(4, result.Count);
            AssertIsProperty(result[0], "Foo");
            AssertIsProperty(result[1], "Bar");
            AssertIsIndexer(result[2], "5", "6");
            AssertIsProperty(result[3], "Baz");
        }

        [Fact]
        public void Should_Parse_Stream_Node()
        {
            var result = Parse("Foo^");

            Assert.Equal(2, result.Count);
            Assert.IsType<BindingExpressionGrammar.StreamNode>(result[1]);
        }

        private static void AssertIsProperty(
            BindingExpressionGrammar.INode node,
            string name,
            bool acceptsNull = false)
        {
            var p = Assert.IsType<BindingExpressionGrammar.PropertyNameNode>(node);
            Assert.Equal(name, p.PropertyName);
            Assert.Equal(acceptsNull, p.AcceptsNull);
        }

        private static void AssertIsAttachedProperty(
            BindingExpressionGrammar.INode node,
            string typeName,
            string name,
            bool acceptsNull = false)
        {
            var p = Assert.IsType<BindingExpressionGrammar.AttachedPropertyNameNode>(node);
            Assert.Equal(typeName, p.TypeName);
            Assert.Equal(name, p.PropertyName);
            Assert.Equal(acceptsNull, p.AcceptsNull);
        }

        private static void AssertIsIndexer(BindingExpressionGrammar.INode node, params string[] args)
        {
            var e = Assert.IsType<BindingExpressionGrammar.IndexerNode>(node);
            Assert.Equal(e.Arguments, args);
        }

        private static List<BindingExpressionGrammar.INode> Parse(string s)
        {
            var r = new CharacterReader(s);
            return BindingExpressionGrammar.Parse(ref r).Nodes;
        }
    }
}
