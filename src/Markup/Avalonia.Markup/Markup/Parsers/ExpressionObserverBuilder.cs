using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data.Core;
using Avalonia.Utilities;

namespace Avalonia.Markup.Parsers
{
    internal static class ExpressionObserverBuilder
    {
        [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
        internal static (ExpressionNode Node, SourceMode Mode) Parse(string expression, bool enableValidation = false, Func<string, string, Type>? typeResolver = null,
            INameScope? nameScope = null)
        {
            if (string.IsNullOrWhiteSpace(expression))
            {
                return (new EmptyExpressionNode(), default);
            }
            
            var reader = new CharacterReader(expression.AsSpan());
            var parser = new ExpressionParser(enableValidation, typeResolver, nameScope);
            var node = parser.Parse(ref reader);

            if (!reader.End)
            {
                throw new ExpressionParseException(reader.Position, "Expected end of expression.");
            }

            return node;
        }

        [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
        public static ExpressionObserver Build(
            object root,
            string expression,
            bool enableDataValidation = false,
            string? description = null,
            Func<string, string, Type>? typeResolver = null)
        {
            return new ExpressionObserver(
                root,
                Parse(expression, enableDataValidation, typeResolver).Node,
                description ?? expression);
        }

        [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
        public static ExpressionObserver Build(
            IObservable<object> rootObservable,
            string expression,
            bool enableDataValidation = false,
            string? description = null,
            Func<string, string, Type>? typeResolver = null)
        {
            _ = rootObservable ?? throw new ArgumentNullException(nameof(rootObservable));

            return new ExpressionObserver(
                rootObservable,
                Parse(expression, enableDataValidation, typeResolver).Node,
                description ?? expression);
        }

        [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
        public static ExpressionObserver Build(
            Func<object> rootGetter,
            string expression,
            IObservable<ValueTuple> update,
            bool enableDataValidation = false,
            string? description = null,
            Func<string, string, Type>? typeResolver = null)
        {
            _ = rootGetter ?? throw new ArgumentNullException(nameof(rootGetter));

            return new ExpressionObserver(
                rootGetter,
                Parse(expression, enableDataValidation, typeResolver).Node,
                update,
                description ?? expression);
        }
    }
}
