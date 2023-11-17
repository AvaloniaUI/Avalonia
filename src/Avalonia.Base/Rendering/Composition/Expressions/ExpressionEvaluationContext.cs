using System.Collections.Generic;

namespace Avalonia.Rendering.Composition.Expressions
{
    internal struct ExpressionEvaluationContext
    {
        public ExpressionVariant StartingValue { get; set; }
        public ExpressionVariant CurrentValue { get; set; }
        public ExpressionVariant FinalValue { get; set; }
        public IExpressionObject Target { get; set; }
        public IExpressionParameterCollection Parameters { get; set; }
        public IExpressionForeignFunctionInterface ForeignFunctionInterface { get; set; }
    }

    internal interface IExpressionObject
    {
        ExpressionVariant GetProperty(string name);
    }

    internal interface IExpressionParameterCollection
    {
        public ExpressionVariant GetParameter(string name);

        public IExpressionObject GetObjectParameter(string name);
    }

    internal interface IExpressionForeignFunctionInterface
    {
        bool Call(string name, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result);
    }
}
