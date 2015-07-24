// -----------------------------------------------------------------------
// <copyright file="ProgressBarStyle.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using System.Reactive.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Primitives;
    using Perspex.Controls.Shapes;
    using Perspex.Layout;
    using Perspex.Media;
    using Perspex.Styling;

    public class ProgressBarStyle : Styles
    {
        public ProgressBarStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ProgressBar>())
                {
                    Setters = new[]
                    {
                        new Setter(ProgressBar.TemplateProperty, ControlTemplate.Create<ProgressBar>(this.Template)),
                        new Setter(ProgressBar.BackgroundProperty, new SolidColorBrush(0xffdddddd)),
                        new Setter(ProgressBar.ForegroundProperty, new SolidColorBrush(0xffbee6fd)),
                    },
                }
            });
        }

        private Control Template(ProgressBar control)
        {
            Border container = new Border
            {
                [~Border.BackgroundProperty] = control[~ProgressBar.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~ProgressBar.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~ProgressBar.BorderThicknessProperty],

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
                            [~Border.BorderBrushProperty] = control[~ProgressBar.BackgroundProperty],
                        },

                        new Border
                        {
                            Name = "PART_Indicator",
                            BorderThickness = 1,
                            HorizontalAlignment = HorizontalAlignment.Left,
                            [~Border.BackgroundProperty] = control[~ProgressBar.ForegroundProperty],
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
