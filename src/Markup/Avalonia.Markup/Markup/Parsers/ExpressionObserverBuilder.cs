using Avalonia.Data.Core;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Text;

namespace Avalonia.Markup.Parsers
{
    public class ExpressionObserverBuilder
    {
        internal static ExpressionNode Parse(string expression, bool enableValidation = false, Func<string, string, Type> typeResolver = null)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return new EmptyExpressionNode();
            }

            var reader = new Reader(expression.AsSpan());
            var parser = new ExpressionParser(enableValidation, typeResolver);
            var node = parser.Parse(ref reader);

            if (!reader.End)
            {
                throw new ExpressionParseException(reader.Position, "Expected end of expression.");
            }

            return node;
        }

        public static ExpressionObserver Build(
            object root,
            string expression,
            bool enableDataValidation = false,
            string description = null,
            Func<string, string, Type> typeResolver = null)
        {
            return new ExpressionObserver(
                root,
                Parse(expression, enableDataValidation, typeResolver),
                description ?? expression);
        }

        public static ExpressionObserver Build(
            IObservable<object> rootObservable,
            string expression,
            bool enableDataValidation = false,
            string description = null,
            Func<string, string, Type> typeResolver = null)
        {
            Contract.Requires<ArgumentNullException>(rootObservable != null);
            return new ExpressionObserver(
                rootObservable,
                Parse(expression, enableDataValidation, typeResolver),
                description ?? expression);
        }


        public static ExpressionObserver Build(
            Func<object> rootGetter,
            string expression,
            IObservable<Unit> update,
            bool enableDataValidation = false,
            string description = null,
            Func<string, string, Type> typeResolver = null)
        {
            Contract.Requires<ArgumentNullException>(rootGetter != null);

            return new ExpressionObserver(
                () => rootGetter(),
                Parse(expression, enableDataValidation, typeResolver),
                update,
                description ?? expression);
        }
    }
}
