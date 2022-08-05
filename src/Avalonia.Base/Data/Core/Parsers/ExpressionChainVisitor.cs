using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Data.Core.Plugins;

namespace Avalonia.Data.Core.Parsers
{
    internal class ExpressionChainVisitor<TIn> : ExpressionVisitor
    {
        private readonly LambdaExpression _rootExpression;
        private readonly List<TypedBindingTrigger<TIn>> _triggers = new();
        private Expression? _head;

        public ExpressionChainVisitor(LambdaExpression expression)
        {
            _rootExpression = expression;
        }

        public static TypedBindingTrigger<TIn>[] BuildTriggers<TOut>(Expression<Func<TIn, TOut>> expression)
        {
            var visitor = new ExpressionChainVisitor<TIn>(expression);
            visitor.Visit(expression);
            return visitor._triggers.ToArray();
        }

        public static Action<TIn, TOut> BuildWriteExpression<TOut>(Expression<Func<TIn, TOut>> expression)
        {
            var property = (expression.Body as MemberExpression)?.Member as PropertyInfo ??
                throw new ArgumentException(
                    $"Cannot create a two-way binding for '{expression}' because the expression does not target a property.",
                    nameof(expression));

            if (property.GetSetMethod() is not MethodInfo setMethod)
                throw new ArgumentException(
                    $"Cannot create a two-way binding for '{expression}' because the property has no setter.",
                    nameof(expression));

            var instanceParam = Expression.Parameter(typeof(TIn), "x");
            var valueParam = Expression.Parameter(typeof(TOut), "value");
            var lambda = Expression.Lambda<Action<TIn, TOut>>(
                Expression.Call(instanceParam, setMethod, valueParam),
                instanceParam,
                valueParam);
            return lambda.Compile();
        }

        protected override Expression VisitBinary(BinaryExpression node)
        {
            var result = base.VisitBinary(node);
            if (node.Left == _head)
                _head = node;
            return result;
        }

        protected override Expression VisitMember(MemberExpression node)
        {
            var result = base.VisitMember(node);

            if (node.Expression is not null &&
                node.Expression == _head &&
                node.Expression.Type.IsValueType == false &&
                node.Member.MemberType == MemberTypes.Property)
            {
                var i = _triggers.Count;
                var trigger = AvaloniaPropertyBindingTrigger<TIn>.TryCreate(i, node, _rootExpression) ??
                    InpcBindingTrigger<TIn>.TryCreate(i, node, _rootExpression);

                if (trigger is not null)
                    _triggers.Add(trigger);

                _head = node;
            }

            return result;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            var result = base.VisitMethodCall(node);

            if (node.Object is not null &&
                node.Object == _head &&
                node.Type.IsValueType == false)
            {
                var i = _triggers.Count;
                var trigger = InccBindingTrigger<TIn>.TryCreate(i, node, _rootExpression) ??
                    AvaloniaPropertyBindingTrigger<TIn>.TryCreate(i, node, _rootExpression) ??
                    InpcBindingTrigger<TIn>.TryCreate(i, node, _rootExpression);

                if (trigger is not null)
                    _triggers.Add(trigger);

                _head = node;
            }

            return result;
        }

        protected override Expression VisitParameter(ParameterExpression node)
        {
            if (node == _rootExpression.Parameters[0])
                _head = node;
            return base.VisitParameter(node);
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            var result = base.VisitUnary(node);
            if (node.Operand == _head)
                _head = node;
            return result;
        }
    }

}
