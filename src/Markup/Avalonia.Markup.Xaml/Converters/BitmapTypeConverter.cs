using System;
using System.ComponentModel;
using System.Globalization;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace Avalonia.Markup.Xaml.Converters
{
    public class BitmapTypeConverter : TypeConverter
    {
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            var s = (string)value;
            var uri = s.StartsWith("/")
                ? new Uri(s, UriKind.Relative)
                : new Uri(s, UriKind.RelativeOrAbsolute);

            if(uri.IsAbsoluteUri && uri.IsFile)
                return new Bitmap(uri.LocalPath);

            var assets = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            return new Bitmap(assets.Open(uri, context?.GetContextBaseUri()));
        }
    }
}
