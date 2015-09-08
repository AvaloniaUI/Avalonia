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
    /// The default style for the <see cref="PopupRoot"/> control.
    /// </summary>
    public class PopupRootStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PopupRootStyle"/> class.
        /// </summary>
        public PopupRootStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<PopupRoot>())
                {
                    Setters = new[]
                    {
                        new Setter(PopupRoot.TemplateProperty, new ControlTemplate<PopupRoot>(Template)),
                        new Setter(PopupRoot.FontFamilyProperty, "Segoe UI"),
                        new Setter(PopupRoot.FontSizeProperty, 12.0),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="PopupRoot"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(PopupRoot control)
        {
            return new Border
            {
                [~Border.BackgroundProperty] = control[~PopupRoot.BackgroundProperty],
                Child = new ContentPresenter
                {
                    Name = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~PopupRoot.ContentProperty],
                }
            };
        }
    }
}
