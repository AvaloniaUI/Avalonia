using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using OmniXaml.TypeConversion;

namespace Avalonia.Markup.Xaml.Converters
{
    class IconTypeConverter : ITypeConverter
    {
        public bool CanConvertFrom(IValueContext context, Type sourceType)
        {
            return sourceType == typeof(string) || typeof(IBitmap).GetTypeInfo().IsAssignableFrom(sourceType.GetTypeInfo());
        }

        public bool CanConvertTo(IValueContext context, Type destinationType)
        {
            return false;
        }

        public object ConvertFrom(IValueContext context, CultureInfo culture, object value)
        {
            var path = value as string;
            if (path != null)
            {
                return CreateIconFromPath(context, path); 
            }
            var bitmap = value as IBitmap;
            if (bitmap != null)
            {
                return CreateIconFromBitmap(bitmap);
            }
            throw new NotSupportedException();
        }

        private WindowIcon CreateIconFromBitmap(IBitmap bitmap)
        {
            return new WindowIcon(bitmap);
        }

        private WindowIcon CreateIconFromPath(IValueContext context, string path)
        {
            var uri = new Uri(path, UriKind.RelativeOrAbsolute);
            var baseUri = GetBaseUri(context);
            var scheme = uri.IsAbsoluteUri ? uri.Scheme : "file";

            switch (scheme)
            {
                case "file":
                    return new WindowIcon(path);
                default:
                    var assets = AvaloniaLocator.Current.GetService<IAssetLoader>();
                    return new WindowIcon(assets.Open(uri, baseUri));
            }
        }

        public object ConvertTo(IValueContext context, CultureInfo culture, object value, Type destinationType)
        {
            throw new NotImplementedException();
        }

        private Uri GetBaseUri(IValueContext context)
        {
            object result;
            context.ParsingDictionary.TryGetValue("Uri", out result);
            return result as Uri;
        }
    }
}
