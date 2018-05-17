using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Text;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    class IndexerExpressionNode : IndexerNodeBase
    {
        private readonly ParameterExpression parameter;
        private readonly IndexExpression expression;
        private readonly Delegate setDelegate;
        private readonly Delegate getDelegate;
        private readonly Delegate firstArgumentDelegate;

        public IndexerExpressionNode(IndexExpression expression)
        {
            parameter = Expression.Parameter(expression.Object.Type);
            this.expression = expression.Update(parameter, expression.Arguments);

            getDelegate = Expression.Lambda(this.expression, parameter).Compile();

            var valueParameter = Expression.Parameter(expression.Type);

            setDelegate = Expression.Lambda(Expression.Assign(this.expression, valueParameter), parameter, valueParameter).Compile();

            firstArgumentDelegate = Expression.Lambda(this.expression.Arguments[0], parameter).Compile();
        }

        public override Type PropertyType => expression.Type;

        public override string Description => expression.ToString();

        public override bool SetTargetValue(object value, BindingPriority priority)
        {
            try
            {
                setDelegate.DynamicInvoke(Target.Target, value);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        protected override object GetValue(object target)
        {
            return getDelegate.DynamicInvoke(target);
        }

        protected override bool ShouldUpdate(object sender, PropertyChangedEventArgs e)
        {
            return expression.Indexer.Name == e.PropertyName;
        }

        protected override int? TryGetFirstArgumentAsInt() => firstArgumentDelegate.DynamicInvoke(Target.Target) as int?;
    }
}
