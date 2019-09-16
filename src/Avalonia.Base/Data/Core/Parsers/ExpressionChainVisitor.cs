using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Avalonia.Data.Core.Parsers
{
    internal class ExpressionChainVisitor<TIn> : ExpressionVisitor
    {
        private readonly LambdaExpression _rootExpression;
        private List<Func<TIn, object>> _links = new List<Func<TIn, object>>();

        public ExpressionChainVisitor(LambdaExpression expression)
        {
            _rootExpression = expression;
        }

        public static List<Func<TIn, object>> Build<TIn, TOut>(Expression<Func<TIn, TOut>> expression)
        {
            var visitor = new ExpressionChainVisitor<TIn>(expression);
            visitor.Visit(expression);
            visitor._links.Reverse();
            return visitor._links;
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
