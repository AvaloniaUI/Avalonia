using System;
using Avalonia.Controls;
using Avalonia.Styling;

namespace Avalonia.Markup.Xaml.MarkupExtensions
{
    internal static class IServiceProviderExtensions
    {
        public static object GetDefaultAnchor(this IServiceProvider provider)
        {
            // If the target is not a control, so we need to find an anchor that will let us look
            // up named controls and style resources. First look for the closest IControl in
            // the context.
            object anchor = provider.GetFirstParent<IControl>();

            if (anchor is null)
            {
                // Try to find IDataContextProvider, this was added to allow us to find
                // a datacontext for Application class when using NativeMenuItems.
                anchor = provider.GetFirstParent<IDataContextProvider>();
            }

            // If a control was not found, then try to find the highest-level style as the XAML
            // file could be a XAML file containing only styles.
            return anchor ??
                   provider.GetService<IRootObjectProvider>()?.RootObject as IStyle ??
                   provider.GetLastParent<IStyle>();
        }
    }
}
