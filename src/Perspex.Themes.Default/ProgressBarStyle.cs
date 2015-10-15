// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using System.Reactive.Linq;
using Perspex.Controls;
using Perspex.Controls.Presenters;
using Perspex.Controls.Primitives;
using Perspex.Controls.Shapes;
using Perspex.Controls.Templates;
using Perspex.Layout;
using Perspex.Media;
using Perspex.Styling;

namespace Perspex.Themes.Default
{
    using Controls = Controls.Controls;

    /// <summary>
    /// The default style for the <see cref="ProgressBar"/> control.
    /// </summary>
    public class ProgressBarStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressBarStyle"/> class.
        /// </summary>
        public ProgressBarStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<ProgressBar>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ProgressBar>(Template)),
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(TemplatedControl.ForegroundProperty, new SolidColorBrush(0xffbee6fd)),
                    },
                }
            });
        }

        /// <summary>
        /// The default template for the <see cref="ProgressBar"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ProgressBar control)
        {
            Border container = new Border
            {
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TemplatedControl.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TemplatedControl.BorderThicknessProperty],

                Child = new Grid
                {
                    MinHeight = 14,
                    MinWidth = 200,

                    Children = new Controls
                    {
                        new Border
                        {
                            Name = "PART_Track",
                            BorderThickness = 1,
                            [~Border.BorderBrushProperty] = control[~TemplatedControl.BackgroundProperty],
                        },

                        new Border
                        {
                            Name = "PART_Indicator",
                            BorderThickness = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            [~Border.BackgroundProperty] = control[~TemplatedControl.ForegroundProperty],
                            Child = new Grid
                            {
                                Name = "Animation",
                                ClipToBounds = true,
                            }
                        }
                    }
                }
            };

            return container;
        }
    }
}
