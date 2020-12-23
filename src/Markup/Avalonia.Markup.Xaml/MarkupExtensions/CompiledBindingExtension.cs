using System;
using Avalonia.Data;
using Avalonia.Controls;
using Avalonia.Styling;
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
                DefaultAnchor = new WeakReference(GetDefaultAnchor(provider))
            };
        }

        private static object GetDefaultAnchor(IServiceProvider provider)
        {
            // If the target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest IControl in
            // the context.
            object anchor = provider.GetFirstParent<IControl>();

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                    provider.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                    provider.GetLastParent<IStyle>();
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
    }
}
