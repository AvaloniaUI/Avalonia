// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
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
            AddRange(new[]
            {
                new Style(x => x.OfType<Window>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<Window>(Template)),
                        new Setter(TemplatedControl.FontFamilyProperty, "Segoe UI"),
                        new Setter(TemplatedControl.FontSizeProperty, 12.0),
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
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                Child = new AdornerDecorator
                {
                    Child = new ContentPresenter
                    {
                        Name = "contentPresenter",
                        [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                    }
                }
            };
        }
    }
}
