using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.ComponentModel;
using System.Globalization;

namespace Avalonia.Markup.Xaml.Converters
{
    public class IconTypeConverter : TypeConverter
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            var path = value as string;
            if (path != null)
            {
                return CreateIconFromPath(context, path);
            }

            var bitmap = value as Bitmap;
            if (bitmap != null)
            {
                return new WindowIcon(bitmap);
            }

            throw new NotSupportedException();
        }

        private static WindowIcon CreateIconFromPath(ITypeDescriptorContext? context, string s)
        {
            var uri = s.StartsWith("/")
                ? new Uri(s, UriKind.Relative)
                : new Uri(s, UriKind.RelativeOrAbsolute);
            
            if(uri.IsAbsoluteUri && uri.IsFile)
                return new WindowIcon(uri.LocalPath);
            var assets = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            return new WindowIcon(assets.Open(uri, context?.GetContextBaseUri()));
        }
    }
}
