using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace Avalonia.Data.Core
{
    class IndexerExpressionNode : IndexerNodeBase
    {
        private readonly ParameterExpression _parameter;
        private readonly IndexExpression _expression;
        private readonly Delegate _setDelegate;
        private readonly Delegate _getDelegate;
        private readonly Delegate _firstArgumentDelegate;

        public IndexerExpressionNode(IndexExpression expression)
        {
            _parameter = Expression.Parameter(expression.Object.Type);
            _expression = expression.Update(_parameter, expression.Arguments);

            _getDelegate = Expression.Lambda(_expression, _parameter).Compile();

            var valueParameter = Expression.Parameter(expression.Type);

            _setDelegate = Expression.Lambda(Expression.Assign(_expression, valueParameter), _parameter, valueParameter).Compile();

            _firstArgumentDelegate = Expression.Lambda(_expression.Arguments[0], _parameter).Compile();
        }

        public override Type PropertyType => _expression.Type;

        public override string Description => _expression.ToString();

        protected override bool SetTargetValueCore(object value, BindingPriority priority)
        {
            try
            {
                Target.TryGetTarget(out object target);

                _setDelegate.DynamicInvoke(target, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override object GetValue(object target)
        {
            try
            {
                return _getDelegate.DynamicInvoke(target);
            }
            catch (TargetInvocationException e) when (e.InnerException is ArgumentOutOfRangeException
                                                        || e.InnerException is IndexOutOfRangeException
                                                        || e.InnerException is KeyNotFoundException)
            {
                return AvaloniaProperty.UnsetValue;
            }
        }

        protected override bool ShouldUpdate(object sender, PropertyChangedEventArgs e)
        {
            return _expression.Indexer == null || _expression.Indexer.Name == e.PropertyName;
        }

        protected override int? TryGetFirstArgumentAsInt()
        {
            Target.TryGetTarget(out object target);

            return _firstArgumentDelegate.DynamicInvoke(target) as int?;
        } 
    }
}
