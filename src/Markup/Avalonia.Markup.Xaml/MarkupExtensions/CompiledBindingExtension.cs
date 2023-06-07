using System;
using Avalonia.Data;
using Avalonia.Markup.Xaml.MarkupExtensions.CompiledBindings;
using Avalonia.Data.Core;
using Avalonia.Markup.Parsers;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    public class CompiledBindingExtension : BindingBase
    {
        public CompiledBindingExtension()
        {
            Path = new CompiledBindingPath();
        }

        public CompiledBindingExtension(CompiledBindingPath path)
        {
            Path = path;
        }

        public CompiledBindingExtension ProvideValue(IServiceProvider provider)
        {
            return new CompiledBindingExtension
            {
                Path = Path,
                Converter = Converter,
                ConverterParameter = ConverterParameter,
                TargetNullValue = TargetNullValue,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Priority = Priority,
                StringFormat = StringFormat,
                Source = Source,
                DefaultAnchor = new WeakReference(provider.GetDefaultAnchor())
            };
        }

        private protected override ExpressionObserver CreateExpressionObserver(AvaloniaObject target, AvaloniaProperty? targetProperty, object? anchor, bool enableDataValidation)
        {
            if (Source != null)
            {
                return CreateSourceObserver(
                    Source,
                    Path.BuildExpression(enableDataValidation));
            }

            if (Path.RawSource != null)
            {
                return CreateSourceObserver(
                    Path.RawSource,
                    Path.BuildExpression(enableDataValidation));
            }

            if (Path.SourceMode == SourceMode.Data)
            {
                return CreateDataContextObserver(
                    target,
                    Path.BuildExpression(enableDataValidation),
                    targetProperty == StyledElement.DataContextProperty,
                    anchor);
            }
            else
            {
                var styledElement = target as StyledElement
                    ?? anchor as StyledElement
                    ?? throw new ArgumentException($"Cannot find a valid {nameof(StyledElement)} to use as the binding source.");

                return CreateSourceObserver(
                    styledElement,
                    Path.BuildExpression(enableDataValidation));
            }
        }

        [ConstructorArgument("path")]
        public CompiledBindingPath Path { get; set; }

        public object? Source { get; set; }

        public Type? DataType { get; set; }
    }
}
