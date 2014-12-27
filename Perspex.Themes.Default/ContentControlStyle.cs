// -----------------------------------------------------------------------
// <copyright file="ContentControlStyle.cs" company="Steven Kirk">
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

    public class ContentControlStyle : Styles
    {
        public ContentControlStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<ContentControl>())
                {
                    Setters = new[]
                    {
                        new Setter(ContentControl.TemplateProperty, ControlTemplate.Create<ContentControl>(this.Template)),
                    },
                },
            });
        }

        private Control Template(ContentControl control)
        {
            return new ContentPresenter
            { 
                Id = "contentPresenter",
                [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
            };
        }
    }
}
