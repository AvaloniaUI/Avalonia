// -----------------------------------------------------------------------
// <copyright file="ToolTipStyle.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Controls.Templates;
    using Perspex.Media;
    using Perspex.Styling;

    public class ToolTipStyle : Styles
    {
        public ToolTipStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ToolTip>())
                {
                    Setters = new[]
                    {
                        new Setter(ToolTip.TemplateProperty, new ControlTemplate<ToolTip>(this.Template)),
                        new Setter(ToolTip.BackgroundProperty, new SolidColorBrush(0xffffffe1)),
                        new Setter(ToolTip.BorderBrushProperty, Brushes.Gray),
                        new Setter(ToolTip.BorderThicknessProperty, 1.0),
                        new Setter(ToolTip.PaddingProperty, new Thickness(4, 2)),
                    },
                },
            });
        }

        private Control Template(ToolTip control)
        {
            return new Border
            {
                [~Border.BackgroundProperty] = control[~ToolTip.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~ToolTip.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~ToolTip.BorderThicknessProperty],
                [~Border.PaddingProperty] = control[~ToolTip.PaddingProperty],
                Child = new ContentPresenter
                {
                    Name = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~ToolTip.ContentProperty],
                }
            };
        }
    }
}
