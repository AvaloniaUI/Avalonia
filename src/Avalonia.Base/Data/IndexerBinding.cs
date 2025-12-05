using System;
using Avalonia.Data.Core;
using Avalonia.Diagnostics;

namespace Avalonia.Data
{
    internal class IndexerBinding : BindingBase
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

        public AvaloniaProperty Property { get; }

        private AvaloniaObject Source { get; }
        private BindingMode Mode { get; }

        internal override BindingExpressionBase CreateInstance(
            AvaloniaObject target,
            AvaloniaProperty? targetProperty,
            object? anchor)
        {
            return new IndexerBindingExpression(Source, Property, target, targetProperty, Mode);
        }
    }
}
