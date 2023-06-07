using System;
using Avalonia.Controls.Platform;
using Avalonia.Reactive;
using Avalonia.Platform;

namespace Avalonia.Controls
{
    public partial class NativeMenu
    {
        public static readonly AttachedProperty<bool> IsNativeMenuExportedProperty =
            AvaloniaProperty.RegisterAttached<NativeMenu, TopLevel, bool>("IsNativeMenuExported");

        public static bool GetIsNativeMenuExported(TopLevel tl) => tl.GetValue(IsNativeMenuExportedProperty);
        
        private static readonly AttachedProperty<NativeMenuInfo?> s_nativeMenuInfoProperty =
            AvaloniaProperty.RegisterAttached<NativeMenu, TopLevel, NativeMenuInfo?>("___NativeMenuInfo");

        private sealed class NativeMenuInfo
        {
            public bool ChangingIsExported { get; set; }
            public ITopLevelNativeMenuExporter? Exporter { get; }

            public NativeMenuInfo(TopLevel target)
            {
                Exporter = target.PlatformImpl?.TryGetFeature<ITopLevelNativeMenuExporter>();
                if (Exporter != null)
                {
                    Exporter.OnIsNativeMenuExportedChanged += delegate
                    {
                        SetIsNativeMenuExported(target, Exporter.IsNativeMenuExported);
                    };
                }
            }
        }

        private static NativeMenuInfo GetInfo(TopLevel target)
        {
            var rv = target.GetValue(s_nativeMenuInfoProperty);
            if (rv == null)
            {
                target.SetValue(s_nativeMenuInfoProperty, rv = new NativeMenuInfo(target));
                SetIsNativeMenuExported(target, rv.Exporter?.IsNativeMenuExported ?? false);
            }

            return rv;
        }

        private static void SetIsNativeMenuExported(TopLevel tl, bool value)
        {
            GetInfo(tl).ChangingIsExported = true;
            tl.SetValue(IsNativeMenuExportedProperty, value);
        }

        public static readonly AttachedProperty<NativeMenu?> MenuProperty
            = AvaloniaProperty.RegisterAttached<NativeMenu, AvaloniaObject, NativeMenu?>("Menu");

        public static void SetMenu(AvaloniaObject o, NativeMenu? menu) => o.SetValue(MenuProperty, menu);

        public static NativeMenu? GetMenu(AvaloniaObject o) => o.GetValue(MenuProperty);
        
        static NativeMenu()
        {
            // This is needed because of the lack of attached direct properties
            IsNativeMenuExportedProperty.Changed.Subscribe(args =>
            {
                var info = GetInfo((TopLevel)args.Sender);
                if (!info.ChangingIsExported)
                    throw new InvalidOperationException("IsNativeMenuExported property is read-only");
                info.ChangingIsExported = false;
            });
            MenuProperty.Changed.Subscribe(args =>
            {
                if (args.Sender is TopLevel tl)
                {
                    GetInfo(tl).Exporter?.SetNativeMenu(args.NewValue.GetValueOrDefault());
                }
                else if(args.Sender is INativeMenuExporterProvider provider)
                {
                    provider.NativeMenuExporter?.SetNativeMenu(args.NewValue.GetValueOrDefault());
                }
            });
        }
    }
}
