using System;
using System.Linq.Expressions;
using System.Reflection;
using Avalonia.Data.Core.Parsers;

namespace Avalonia.Data
{
    /// <summary>
    /// Provides factory methods for creating <see cref="TypedBinding{TIn, TOut}"/> objects from
    /// C# lambda expressions.
    /// </summary>
    /// <typeparam name="TIn">The input type of the binding.</typeparam>
    public static class TypedBinding<TIn>
        where TIn : class
    {
        public static TypedBinding<TIn, TOut> Default<TOut>(
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write)
        {
            return new TypedBinding<TIn, TOut>
            {
                Read = read.Compile(),
                Write = write,
                ReadTriggers = ExpressionChainVisitor<TIn>.BuildTriggers(read),
                Mode = BindingMode.Default,
            };
        }

        public static TypedBinding<TIn, TOut> OneWay<TOut>(Expression<Func<TIn, TOut>> read)
        {
            return new TypedBinding<TIn, TOut>
            {
                Read = read.Compile(),
                ReadTriggers = ExpressionChainVisitor<TIn>.BuildTriggers(read),
            };
        }

        public static TypedBinding<TIn, TOut> TwoWay<TOut>(Expression<Func<TIn, TOut>> expression)
        {
            return new TypedBinding<TIn, TOut>
            {
                Read = expression.Compile(),
                Write = ExpressionChainVisitor<TIn>.BuildWriteExpression(expression),
                ReadTriggers = ExpressionChainVisitor<TIn>.BuildTriggers(expression),
                Mode = BindingMode.TwoWay,
            };
        }

        public static TypedBinding<TIn, TOut> TwoWay<TOut>(
            Expression<Func<TIn, TOut>> read,
            Action<TIn, TOut> write)
        {
            return new TypedBinding<TIn, TOut>
            {
                Read = read.Compile(),
                Write = write,
                ReadTriggers = ExpressionChainVisitor<TIn>.BuildTriggers(read),
                Mode = BindingMode.TwoWay,
            };
        }

        public static TypedBinding<TIn, TOut> OneTime<TOut>(Expression<Func<TIn, TOut>> read)
        {
            return new TypedBinding<TIn, TOut>
            {
                Read = read.Compile(),
                ReadTriggers = ExpressionChainVisitor<TIn>.BuildTriggers(read),
                Mode = BindingMode.OneTime,
            };
        }

        public static TypedBinding<TIn, TOut> OneWayToSource<TOut>(Action<TIn, TOut> write)
        {
            return new TypedBinding<TIn, TOut>
            {
                Write = write,
                Mode = BindingMode.OneWayToSource,
            };
        }
    }
}
