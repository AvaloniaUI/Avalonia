using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Data;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    [RequiresUnreferencedCode(TrimmingMessages.ReflectionBindingRequiresUnreferencedCodeMessage)]
#if NET8_0_OR_GREATER
    [RequiresDynamicCode(TrimmingMessages.ReflectionBindingRequiresDynamicCodeMessage)]
#endif
    public sealed class ReflectionBindingExtension : ReflectionBinding
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        public ReflectionBindingExtension() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        public ReflectionBindingExtension(string path) : base(path) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReflectionBinding"/> class.
        /// </summary>
        /// <param name="path">The binding path.</param>
        /// <param name="mode">The binding mode.</param>
        public ReflectionBindingExtension(string path, BindingMode mode) : base(path, mode) { }

        public ReflectionBinding ProvideValue(IServiceProvider serviceProvider)
        {
            return new ReflectionBinding
            {
                TypeResolver = serviceProvider.ResolveType,
                Converter = Converter,
                ConverterCulture = ConverterCulture,
                ConverterParameter = ConverterParameter,
                ElementName = ElementName,
                FallbackValue = FallbackValue,
                Mode = Mode,
                Path = Path,
                Priority = Priority,
                Delay = Delay,
                Source = Source,
                StringFormat = StringFormat,
                RelativeSource = RelativeSource,
                DefaultAnchor = new WeakReference(serviceProvider.GetDefaultAnchor()),
                TargetNullValue = TargetNullValue,
                NameScope = new WeakReference<INameScope?>(serviceProvider.GetService<INameScope>()),
                UpdateSourceTrigger = UpdateSourceTrigger,
            };
        }
    }
}
