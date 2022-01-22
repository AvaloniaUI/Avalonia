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

        protected override ExpressionObserver CreateExpressionObserver(IAvaloniaObject target, AvaloniaProperty targetProperty, object anchor, bool enableDataValidation)
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
                return CreateSourceObserver(
                    (target as IStyledElement) ?? (anchor as IStyledElement),
                    Path.BuildExpression(enableDataValidation));
            }
        }

        [ConstructorArgument("path")]
        public CompiledBindingPath Path { get; set; }

        public object Source { get; set; }

        public Type DataType { get; set; }
    }
}
