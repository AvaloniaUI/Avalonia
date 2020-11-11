using System;
using System.Collections.Generic;
using System.Linq.Expressions;

#nullable enable

namespace Avalonia.Data.Core.Parsers
{
    public class ExpressionChainVisitor<TIn> : ExpressionVisitor
    {
        private readonly LambdaExpression _rootExpression;
        private List<Func<TIn, object>> _links = new List<Func<TIn, object>>();

        public ExpressionChainVisitor(LambdaExpression expression)
        {
            _rootExpression = expression;
        }

        public static Func<TIn, object>[] Build<TOut>(Expression<Func<TIn, TOut>> expression)
        {
            var visitor = new ExpressionChainVisitor<TIn>(expression);
            visitor.Visit(expression);
            visitor._links.Reverse();
            return visitor._links.ToArray();
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            if (node.Expression?.GetType().IsValueType == false)
            {
                var link = Expression.Lambda<Func<TIn, object>>(node.Expression, _rootExpression.Parameters);
                _links.Add(link.Compile());
            }

            return base.VisitMember(node);
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Object?.GetType().IsValueType == false)
            {
                var link = Expression.Lambda<Func<TIn, object>>(node.Object, _rootExpression.Parameters);
                _links.Add(link.Compile());
            }

            return base.VisitMethodCall(node);
        }
    }
}
