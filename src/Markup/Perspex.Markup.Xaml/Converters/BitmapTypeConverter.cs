// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Globalization;
using OmniXaml.TypeConversion;
using Perspex.Media.Imaging;
using Perspex.Platform;

namespace Perspex.Markup.Xaml.Converters
{
    public class BitmapTypeConverter : ITypeConverter
    {
        public bool CanConvertFrom(IValueContext context, Type sourceType)
        {
            return sourceType == typeof(string);
        }

        public bool CanConvertTo(IValueContext context, Type destinationType)
        {
            return false;
        }

        public object ConvertFrom(IValueContext context, CultureInfo culture, object value)
        {
            var uri = new Uri((string)value, UriKind.RelativeOrAbsolute);
            var baseUri = GetBaseUri(context);
            var scheme = uri.IsAbsoluteUri ? uri.Scheme : "file";

            switch (scheme)
            {
                case "file":
                    return new Bitmap((string)value);
                default:
                    var assets = PerspexLocator.Current.GetService<IAssetLoader>();
                    return new Bitmap(assets.Open(uri, baseUri));
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