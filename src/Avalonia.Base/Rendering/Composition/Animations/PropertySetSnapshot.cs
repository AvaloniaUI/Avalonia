using System.Collections.Generic;
using Avalonia.Rendering.Composition.Expressions;

namespace Avalonia.Rendering.Composition.Animations
{
    /// <summary>
    /// A snapshot of properties used by an animation
    /// </summary>
    internal class PropertySetSnapshot : IExpressionParameterCollection, IExpressionObject
    {
        private readonly Dictionary<string, Value> _dic;

        public struct Value
        {
            public ExpressionVariant Variant;
            public IExpressionObject Object;

            public Value(IExpressionObject o)
            {
                Object = o;
                Variant = default;
            }

            public static implicit operator Value(ExpressionVariant v) => new Value
            {
                Variant = v
            };
        }

        public PropertySetSnapshot(Dictionary<string, Value> dic)
        {
            _dic = dic;
        }

        public ExpressionVariant GetParameter(string name)
        {
            _dic.TryGetValue(name, out var v);
            return v.Variant;
        }

        public IExpressionObject GetObjectParameter(string name)
        {
            _dic.TryGetValue(name, out var v);
            return v.Object;
        }

        public ExpressionVariant GetProperty(string name) => GetParameter(name);
    }
}
