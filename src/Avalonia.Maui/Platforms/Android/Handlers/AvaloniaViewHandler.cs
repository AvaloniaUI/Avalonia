using Avalonia.Android;
using Avalonia.Maui.Platforms.Android.Handlers;
using Microsoft.Maui.Handlers;
using static Android.Provider.MediaStore;
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
    }
}
