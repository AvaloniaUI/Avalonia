// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Perspex.Controls;
using Perspex.Controls.Primitives;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    /// <summary>
    /// The default style for the <see cref="GridSplitter"/> control.
    /// </summary>
    public class GridSplitterStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GridSplitterStyle"/> class.
        /// </summary>
        public GridSplitterStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<GridSplitter>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<GridSplitter>(Template)),
                        new Setter(Layoutable.WidthProperty, 4.0),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for a <see cref="GridSplitter"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(GridSplitter control)
        {
            Border border = new Border
            {
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
            };

            return border;
        }
    }
}
