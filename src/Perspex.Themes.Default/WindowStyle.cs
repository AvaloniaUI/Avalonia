﻿// -----------------------------------------------------------------------
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

    public class WindowStyle : Styles
    {
        public WindowStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Window>())
                {
                    Setters = new[]
                    {
                        new Setter(Window.TemplateProperty, new ControlTemplate<Window>(this.Template)),
                        new Setter(Window.FontFamilyProperty, "Segoe UI"),
                        new Setter(Window.FontSizeProperty, 12.0),
                    },
                },
            });
        }

        private Control Template(Window control)
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
