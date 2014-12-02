// -----------------------------------------------------------------------
// <copyright file="TextService.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Cairo.Media
{
    using System;
    using System.Linq;
    using System.Runtime.InteropServices;
    using Perspex.Media;
    using Perspex.Platform;

    public class TextService : ITextService
    {
        /// <summary>
        /// Gets the pango context to be used by the service.
        /// </summary>
        /// <remarks>>
        /// There seems to be no way in GtkSharp to create a new Pango Context, so this has to
        /// be injected by CairoPlatform the first time a renderer is created.
        /// </remarks>
        public Pango.Context Context
        {
            get;
            internal set;
        }

        public Pango.Layout CreateLayout(FormattedText text)
        {
            var layout = new Pango.Layout(this.Context)
                {
                    FontDescription = new Pango.FontDescription
                        {
                            Family = text.FontFamilyName,
                            Size = Pango.Units.FromDouble(text.FontSize),
                            Style = (Pango.Style)text.FontStyle,
                        }
                };

            layout.SetText(text.Text);

            return layout;
        }

        public int GetCaretIndex(FormattedText text, Point point, Size constraint)
        {
            throw new NotImplementedException();
        }

        public Point GetCaretPosition(FormattedText text, int caretIndex, Size constraint)
        {
            throw new NotImplementedException();
        }

        public double[] GetLineHeights(FormattedText text, Size constraint)
        {
            throw new NotImplementedException();
        }

        public Size Measure(FormattedText text, Size constraint)
        {
            var layout = this.CreateLayout(text);

            Pango.Rectangle inkRect;
            Pango.Rectangle logicalRect;
            layout.GetExtents(out inkRect, out logicalRect);

            return new Size(Pango.Units.ToDouble(logicalRect.Width), Pango.Units.ToDouble(logicalRect.Height));
        }
    }
}
