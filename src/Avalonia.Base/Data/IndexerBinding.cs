using System;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;

namespace Avalonia.Data
{
    internal class IndexerBinding : IBinding2
    {
        public IndexerBinding(
            AvaloniaObject source,
            AvaloniaProperty property,
            BindingMode mode)
        {
            Source = source;
            Property = property;
            Mode = mode;
        }

        private AvaloniaObject Source { get; }
        public AvaloniaProperty Property { get; }
        private BindingMode Mode { get; }

        [Obsolete(ObsoletionMessages.MayBeRemovedInAvalonia12)]
        public InstancedBinding? Initiate(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor = null,
            bool enableDataValidation = false)
        {
            var expression = new IndexerBindingExpression(Source, Property, target, targetProperty, Mode);
            return new InstancedBinding(expression, Mode, BindingPriority.LocalValue);
        }

        BindingExpressionBase IBinding2.Instance(AvaloniaObject target, AvaloniaProperty targetProperty, object? anchor)
        {
            return new IndexerBindingExpression(Source, Property, target, targetProperty, Mode);
        }
    }
}
