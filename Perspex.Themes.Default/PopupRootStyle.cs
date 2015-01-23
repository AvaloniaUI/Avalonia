// -----------------------------------------------------------------------
// <copyright file="PopupRootStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Controls.Presenters;
    using Perspex.Media;
    using Perspex.Styling;

    public class PopupRootStyle : Styles
    {
        public PopupRootStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<PopupRoot>())
                {
                    Setters = new[]
                    {
                        new Setter(PopupRoot.TemplateProperty, ControlTemplate.Create<PopupRoot>(this.Template)),
                        new Setter(PopupRoot.FontFamilyProperty, "Segoe UI"),
                        new Setter(PopupRoot.FontSizeProperty, 12.0),
                    },
                },
            });
        }

        private Control Template(PopupRoot control)
        {
            return new Border
            {
                [~Border.BackgroundProperty] = control[~PopupRoot.BackgroundProperty],
                Content = new ContentPresenter
                {
                    Id = "contentPresenter",
                    [~ContentPresenter.ContentProperty] = control[~PopupRoot.ContentProperty],
                }
            };
        }
    }
}
