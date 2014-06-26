// -----------------------------------------------------------------------
// <copyright file="FormattedText.cs" company="Steven Kirk">
// Copyright 2013 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Media
{
    using Perspex.Platform;
    using Splat;

    public class FormattedText
    {
        public string FontFamilyName { get; set; }

        public double FontSize { get; set; }

        public string Text { get; set; }

        public Size Size
        {
            get
            {
                IPlatformRenderInterface factory = Locator.Current.GetService<IPlatformRenderInterface>();
                return factory.TextService.Measure(this);
            }
        }
    }
}
