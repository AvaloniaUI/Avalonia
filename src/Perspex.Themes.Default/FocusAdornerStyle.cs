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
    using Perspex.Controls.Templates;
    using Perspex.Media;
    using Perspex.Styling;

    /// <summary>
    /// The default style for a focus adorner.
    /// </summary>
    public class FocusAdornerStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FocusAdornerStyle"/> class.
        /// </summary>
        public FocusAdornerStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.Is<Control>())
                {
                    Setters = new[]
                    {
                        new Setter(Control.FocusAdornerProperty, new FuncTemplate<IControl>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for focus adorner.
        /// </summary>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template()
        {
            return new Rectangle
            {
                Stroke = Brushes.Black,
                StrokeThickness = 1,
                StrokeDashArray = new PerspexList<double>(1, 2),
            };
        }
    }
}
