using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Avalonia.Data;
using Avalonia.Data.Core;
using Avalonia.Data.Core.ExpressionNodes;
using Avalonia.Data.Core.ExpressionNodes.Reflection;
using Avalonia.Data.Core.Parsers;
using Xunit;

#nullable enable

namespace Avalonia.Base.UnitTests.Data.Core.Parsers
{
    public class BindingExpressionVisitorTests
    {
        [Fact]
        public void BuildNodes_Should_Parse_Simple_Property()
        {
            Expression<Func<TestClass, string?>> expr = x => x.StringProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            var node = Assert.Single(nodes);
            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(node);
            Assert.Equal("StringProperty", propertyNode.PropertyName);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Property_Chain()
        {
            Expression<Func<TestClass, string?>> expr = x => x.Child!.StringProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var firstNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("Child", firstNode.PropertyName);

            var secondNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[1]);
            Assert.Equal("StringProperty", secondNode.PropertyName);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Long_Property_Chain()
        {
            Expression<Func<TestClass, string?>> expr = x => x.Child!.Child!.StringProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(3, nodes.Count);
            Assert.All(nodes, n => Assert.IsType<DynamicPluginPropertyAccessorNode>(n));
            Assert.Equal("Child", ((DynamicPluginPropertyAccessorNode)nodes[0]).PropertyName);
            Assert.Equal("Child", ((DynamicPluginPropertyAccessorNode)nodes[1]).PropertyName);
            Assert.Equal("StringProperty", ((DynamicPluginPropertyAccessorNode)nodes[2]).PropertyName);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Indexer()
        {
            Expression<Func<TestClass, TestClass?>> expr = x => x.IndexedProperty![0];

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("IndexedProperty", propertyNode.PropertyName);

            Assert.IsType<ExpressionTreeIndexerNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Array_Index()
        {
            Expression<Func<TestClass, string?>> expr = x => x.ArrayProperty![0];

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("ArrayProperty", propertyNode.PropertyName);

            Assert.IsType<ExpressionTreeIndexerNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Multi_Dimensional_Array()
        {
            Expression<Func<TestClass, string?>> expr = x => x.MultiDimensionalArray![0, 1];

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("MultiDimensionalArray", propertyNode.PropertyName);

            Assert.IsType<ExpressionTreeIndexerNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_AvaloniaProperty_Access()
        {
            Expression<Func<StyledElement, object?>> expr = x => x[StyledElement.DataContextProperty];

            var nodes = BindingExpressionVisitor<StyledElement>.BuildNodes(expr);

            var node = Assert.Single(nodes);
            var avaloniaPropertyNode = Assert.IsType<AvaloniaPropertyAccessorNode>(node);
            Assert.Equal(StyledElement.DataContextProperty, avaloniaPropertyNode.Property);
        }

        [Fact]
        public void BuildNodes_Should_Parse_AvaloniaProperty_Access_In_Chain()
        {
            Expression<Func<TestClass, object?>> expr = x => x.StyledChild![StyledElement.DataContextProperty];

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("StyledChild", propertyNode.PropertyName);

            var avaloniaPropertyNode = Assert.IsType<AvaloniaPropertyAccessorNode>(nodes[1]);
            Assert.Equal(StyledElement.DataContextProperty, avaloniaPropertyNode.Property);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Logical_Not()
        {
            Expression<Func<TestClass, bool>> expr = x => !x.BoolProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("BoolProperty", propertyNode.PropertyName);

            Assert.IsType<LogicalNotNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Logical_Not_In_Chain()
        {
            Expression<Func<TestClass, bool>> expr = x => !x.Child!.BoolProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(3, nodes.Count);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[1]);
            Assert.IsType<LogicalNotNode>(nodes[2]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Task_StreamBinding()
        {
            Expression<Func<TestClass, string?>> expr = x => x.TaskProperty!.StreamBinding();

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("TaskProperty", propertyNode.PropertyName);

            Assert.IsType<DynamicPluginStreamNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Observable_StreamBinding()
        {
            Expression<Func<TestClass, int>> expr = x => x.ObservableProperty!.StreamBinding();

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("ObservableProperty", propertyNode.PropertyName);

            Assert.IsType<DynamicPluginStreamNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Void_Task_StreamBinding()
        {
            Expression<Func<TestClass, object>> expr = x => x.VoidTaskProperty!.StreamBinding();

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("VoidTaskProperty", propertyNode.PropertyName);

            Assert.IsType<DynamicPluginStreamNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Ignore_Upcast()
        {
            // Upcasts (derived to base) are safe and should be ignored
            Expression<Func<DerivedTestClass, TestClass>> expr = x => (TestClass)x;

            var nodes = BindingExpressionVisitor<DerivedTestClass>.BuildNodes(expr);

            Assert.Empty(nodes);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Downcast()
        {
            // Downcasts (base to derived) are not safe and should throw an exception
            Expression<Func<TestClass, DerivedTestClass>> expr = x => (DerivedTestClass)x;

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Convert", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Value_Type_Cast()
        {
            // Value type conversions should throw
            Expression<Func<TestClass, long>> expr = x => (long)x.IntProperty;

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Convert", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Ignore_TypeAs_Operator()
        {
            Expression<Func<TestClass, object?>> expr = x => x.Child as object;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            var node = Assert.Single(nodes);
            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(node);
            Assert.Equal("Child", propertyNode.PropertyName);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Addition_Operator()
        {
            Expression<Func<TestClass, int>> expr = x => x.IntProperty + 1;

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Add", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Subtraction_Operator()
        {
            Expression<Func<TestClass, int>> expr = x => x.IntProperty - 1;

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Subtract", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Multiplication_Operator()
        {
            Expression<Func<TestClass, int>> expr = x => x.IntProperty * 2;

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Multiply", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Equality_Operator()
        {
            Expression<Func<TestClass, bool>> expr = x => x.IntProperty == 42;

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Equal", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Conditional_Expression()
        {
            Expression<Func<TestClass, string?>> expr = x => x.BoolProperty ? "true" : "false";

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Conditional", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Method_Call_That_Is_Not_Indexer_Or_StreamBinding()
        {
            Expression<Func<TestClass, string?>> expr = x => x.StringProperty!.ToUpper();

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid method call", ex.Message);
            Assert.Contains("ToUpper", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Handle_Unary_Plus_Operator()
        {
            // Unary plus is typically optimized away by the C# compiler and doesn't appear in the
            // expression tree, so it doesn't throw an exception.
            Expression<Func<TestClass, int>> expr = x => +x.IntProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            var node = Assert.Single(nodes);
            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(node);
            Assert.Equal("IntProperty", propertyNode.PropertyName);
        }

        [Fact]
        public void BuildNodes_Should_Throw_For_Unary_Minus_Operator()
        {
            Expression<Func<TestClass, int>> expr = x => -x.IntProperty;

            var ex = Assert.Throws<ExpressionParseException>(() =>
                BindingExpressionVisitor<TestClass>.BuildNodes(expr));

            Assert.Contains("Invalid expression type", ex.Message);
            Assert.Contains("Negate", ex.Message);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Chained_Indexers()
        {
            Expression<Func<TestClass, string?>> expr = x => x.NestedIndexedProperty![0]![1];

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(3, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("NestedIndexedProperty", propertyNode.PropertyName);

            Assert.IsType<ExpressionTreeIndexerNode>(nodes[1]);
            Assert.IsType<ExpressionTreeIndexerNode>(nodes[2]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Property_After_Indexer()
        {
            Expression<Func<TestClass, string?>> expr = x => x.IndexedProperty![0]!.StringProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(3, nodes.Count);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.IsType<ExpressionTreeIndexerNode>(nodes[1]);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[2]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Indexer_With_String_Key()
        {
            Expression<Func<TestClass, int>> expr = x => x.DictionaryProperty!["key"];

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);

            var propertyNode = Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.Equal("DictionaryProperty", propertyNode.PropertyName);

            Assert.IsType<ExpressionTreeIndexerNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Indexer_With_Variable_Key()
        {
            var key = "test";
            Expression<Func<TestClass, int>> expr = x => x.DictionaryProperty![key];

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(2, nodes.Count);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.IsType<ExpressionTreeIndexerNode>(nodes[1]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_StreamBinding_In_Property_Chain()
        {
            Expression<Func<TestClass, string?>> expr = x => x.Child!.TaskProperty!.StreamBinding();

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(3, nodes.Count);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[1]);
            Assert.IsType<DynamicPluginStreamNode>(nodes[2]);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Logical_Not_After_StreamBinding()
        {
            Expression<Func<TestClass, bool>> expr = x => !x.BoolTaskProperty!.StreamBinding();

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(3, nodes.Count);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.IsType<DynamicPluginStreamNode>(nodes[1]);
            Assert.IsType<LogicalNotNode>(nodes[2]);
        }

        [Fact]
        public void BuildNodes_Should_Handle_Empty_Expression()
        {
            Expression<Func<TestClass, TestClass>> expr = x => x;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Empty(nodes);
        }

        [Fact]
        public void BuildNodes_Should_Parse_Multiple_Logical_Not_Operators()
        {
            Expression<Func<TestClass, bool>> expr = x => !!x.BoolProperty;

            var nodes = BindingExpressionVisitor<TestClass>.BuildNodes(expr);

            Assert.Equal(3, nodes.Count);
            Assert.IsType<DynamicPluginPropertyAccessorNode>(nodes[0]);
            Assert.IsType<LogicalNotNode>(nodes[1]);
            Assert.IsType<LogicalNotNode>(nodes[2]);
        }

        public class TestClass
        {
            public string? StringProperty { get; set; }
            public int IntProperty { get; set; }
            public bool BoolProperty { get; set; }
            public TestClass? Child { get; set; }
            public StyledElement? StyledChild { get; set; }
            public string?[]? ArrayProperty { get; set; }
            public string?[,]? MultiDimensionalArray { get; set; }
            public List<TestClass>? IndexedProperty { get; set; }
            public List<List<string>>? NestedIndexedProperty { get; set; }
            public Dictionary<string, int>? DictionaryProperty { get; set; }
            public Task<string?>? TaskProperty { get; set; }
            public Task? VoidTaskProperty { get; set; }
            public Task<bool>? BoolTaskProperty { get; set; }
            public IObservable<int>? ObservableProperty { get; set; }
        }

        public class DerivedTestClass : TestClass
        {
        }
    }
}
