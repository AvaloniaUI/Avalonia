#if ANDROID
using Avalonia.Android;
#endif
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Maui
{
    public static class AppHostBuilderExtensions
    {
        public static MauiAppBuilder UseAvalonia<TApp>(this MauiAppBuilder builder, Action<AppBuilder> customerBuilder) where TApp : Application, new()
        {
            var avaloniaBuilder = AppBuilder.Configure<TApp>();
            customerBuilder(avaloniaBuilder);
#if ANDROID
            avaloniaBuilder.UseAndroid();
            var lifetime = new SingleViewLifetime();

            avaloniaBuilder.SetupWithLifetime(lifetime);
#endif

            return builder;
        }
    }
}
