using Avalonia.Android;
using Avalonia.Layout;
using Avalonia.Maui.Platforms.Android.Handlers;
using Microsoft.Maui.Handlers;
using AvaloniaView = Avalonia.Maui.Controls.AvaloniaView;

namespace Avalonia.Maui.Handlers
{
    public partial class AvaloniaViewHandler : ViewHandler<AvaloniaView, MauiAvaloniaView>
    {
        protected override MauiAvaloniaView CreatePlatformView()
        {
            return new MauiAvaloniaView(Context, VirtualView);
        }

        protected override void ConnectHandler(MauiAvaloniaView platformView)
        {
            base.ConnectHandler(platformView);

            platformView.Prepare();

            platformView.Content = VirtualView.Content;

            if (Avalonia.Application.Current.ApplicationLifetime is SingleViewLifetime lifetime)
            {
                lifetime.View = platformView;
            }
        }

        protected override void DisconnectHandler(MauiAvaloniaView platformView)
        {
            platformView.Dispose();
            base.DisconnectHandler(platformView);
        }

        public static void MapContent(AvaloniaViewHandler handler, AvaloniaView view)
        {
            handler.PlatformView?.UpdateContent();
        }

        public override Microsoft.Maui.Graphics.Size GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            if ((VirtualView.VerticalOptions.Alignment != LayoutAlignment.Fill || VirtualView.HorizontalOptions.Alignment != LayoutAlignment.Fill) && VirtualView.Content is Layoutable control)
            {
                control.Measure(new Size(widthConstraint, heightConstraint));

                var displayInfo = DeviceDisplay.MainDisplayInfo;

                var size = new Size(VirtualView.VerticalOptions.Alignment == LayoutAlignment.Fill ? widthConstraint : control.DesiredSize.Width,
                    VirtualView.HorizontalOptions.Alignment == LayoutAlignment.Fill ? heightConstraint : control.DesiredSize.Height);

                base.GetDesiredSize(size.Width, size.Height);

                return new Microsoft.Maui.Graphics.Size(size.Width, size.Height);
            }
            else
            {
                return base.GetDesiredSize(widthConstraint, heightConstraint);
            }
        }
    }
}
