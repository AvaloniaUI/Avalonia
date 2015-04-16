// -----------------------------------------------------------------------
// <copyright file="FocusAdornerStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using Perspex.Collections;
    using Perspex.Controls;
    using Perspex.Controls.Shapes;
    using Perspex.Media;
    using Perspex.Styling;

    public class FocusAdornerStyle : Styles
    {
        public FocusAdornerStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.Is<Control>())
                {
                    Setters = new[]
                    {
                        new Setter(Control.FocusAdornerProperty, new AdornerTemplate(this.Template)),
                    },
                },
            });
        }

        private Control Template()
        {
            return new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                StrokeDashArray = new PerspexList<double>(1, 2),
                Margin = new Thickness(3),
            };
        }
    }
}
