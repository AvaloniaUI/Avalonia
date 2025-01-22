using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Layout;
using Avalonia.Platform;

namespace Avalonia.Styling
{
    public class Container
    {
        public static readonly AttachedProperty<string?> NameProperty =
            AvaloniaProperty.RegisterAttached<Container, Layoutable, string?>("Name");

        public static readonly AttachedProperty<ContainerSizing> SizingProperty =
            AvaloniaProperty.RegisterAttached<Container, Layoutable, ContainerSizing>("Sizing", coerce:UpdateQueryProvider);

        private static ContainerSizing UpdateQueryProvider(AvaloniaObject obj, ContainerSizing sizing)
        {
            if (obj is Layoutable layoutable)
            {
                if (sizing != ContainerSizing.Normal)
                {
                    if (GetQueryProvider(layoutable) == null)
                        layoutable.SetValue(QueryProviderProperty, new VisualQueryProvider(layoutable));
                }
                else
                {
                    layoutable.SetValue(QueryProviderProperty, null);
                }
            }

            return sizing;
        }

        internal static readonly AttachedProperty<VisualQueryProvider?> QueryProviderProperty =
            AvaloniaProperty.RegisterAttached<Container, Layoutable, VisualQueryProvider?>("QueryProvider");

        public static string? GetName(Layoutable layoutable)
        {
            return layoutable.GetValue(NameProperty);
        }

        public static void SetName(Layoutable layoutable, string? name)
        {
            layoutable.SetValue(NameProperty, name);
        }

        public static ContainerSizing GetSizing(Layoutable layoutable)
        {
            return layoutable.GetValue(SizingProperty);
        }

        public static void SetSizing(Layoutable layoutable, ContainerSizing sizing)
        {
            layoutable.SetValue(SizingProperty, sizing);
        }

        internal static VisualQueryProvider? GetQueryProvider(Layoutable layoutable)
        {
            return layoutable.GetValue(QueryProviderProperty);
        }
    }
}
