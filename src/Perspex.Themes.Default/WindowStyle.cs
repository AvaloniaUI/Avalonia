// -----------------------------------------------------------------------
// <copyright file="WindowStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Templates;
    using Perspex.Styling;

    /// <summary>
    /// The default style for the <see cref="Window"/> control.
    /// </summary>
    public class WindowStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WindowStyle"/> class.
        /// </summary>
        public WindowStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Window>())
                {
                    Setters = new[]
                    {
                        new Setter(Window.TemplateProperty, new ControlTemplate<Window>(Template)),
                        new Setter(Window.FontFamilyProperty, "Segoe UI"),
                        new Setter(Window.FontSizeProperty, 12.0),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="Window"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(Window control)
        {
            return new Border
            {
                [~Border.BackgroundProperty] = control[~Window.BackgroundProperty],
                Child = new AdornerDecorator
                {
                    Child = new ContentPresenter
                    {
                        Name = "contentPresenter",
                        [~ContentPresenter.ContentProperty] = control[~Window.ContentProperty],
                    }
                }
            };
        }
    }
}
