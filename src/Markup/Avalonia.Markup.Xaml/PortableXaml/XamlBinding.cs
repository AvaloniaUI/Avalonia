using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Styling;
using Portable.Xaml;
using Portable.Xaml.ComponentModel;
using System.ComponentModel;
using Portable.Xaml.Markup;
using System;

namespace Avalonia.Markup.Xaml.PortableXaml
{
    internal class XamlBinding : IBinding
    {
        public static IBinding FromMarkupExtensionContext(
                                    IBinding binding,
                                    IServiceProvider serviceProvider)
        {
            var context = (ITypeDescriptorContext)serviceProvider;
            var pvt = context.GetService<IProvideValueTarget>();

            if (pvt.TargetObject is IControl) return binding;

            object anchor = GetDefaultAnchor(context);

            if (anchor == null) return binding;

            return new XamlBinding(binding, anchor);
        }

        private static object GetDefaultAnchor(ITypeDescriptorContext context)
        {
            object anchor = null;

            // The target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest IControl in
            // the context.
            anchor = context.GetFirstAmbientValue<IControl>();

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                    context.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                    context.GetLastOrDefaultAmbientValue<IStyle>();
        }

        private XamlBinding(IBinding binding, object anchor)
        {
            Value = binding;

            Anchor = new WeakReference(anchor);
        }

        public WeakReference Anchor { get; }

        public IBinding Value { get; }

        public InstancedBinding Initiate(IAvaloniaObject target, AvaloniaProperty targetProperty, object anchor = null, bool enableDataValidation = false)
        {
            return Value.Initiate(target, targetProperty,
                            anchor ?? Anchor.Target, enableDataValidation);
        }
    }
}