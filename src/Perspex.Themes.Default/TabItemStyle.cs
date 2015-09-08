// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Media;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    /// <summary>
    /// The default style for the <see cref="TabItem"/> control.
    /// </summary>
    public class TabItemStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TabItemStyle"/> class.
        /// </summary>
        public TabItemStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<TabItem>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.FontSizeProperty, 28.7),
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.Gray),
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<TabItem>(Template)),
                    },
                },
                new Style(x => x.OfType<TabItem>().Class("selected"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.ForegroundProperty, Brushes.Black),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="TabItem"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(TabItem control)
        {
            return new ContentPresenter
            {
                Name = "headerPresenter",
                [~ContentPresenter.ContentProperty] = control[~HeaderedContentControl.HeaderProperty],
            };
        }
    }
}
