// -----------------------------------------------------------------------
// <copyright file="TabStyle.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Themes.Default
{
    using System.Linq;
    using Perspex.Controls;
    using Perspex.Media;
    using Perspex.Styling;

    public class TabStyle : Styles
    {
        public TabStyle()
        {
            this.AddRange(new[]
            {
                new Style(x => x.OfType<Tab>())
                {
                    Setters = new[]
                    {
                        new Setter(Button.TemplateProperty, ControlTemplate.Create<Tab>(this.Template)),
                    },
                },
            });
        }

        private Control Template(Tab control)
        {
            return new Border
            {
                Background = Brushes.Red,
                Content = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = control[~Tab.ContentProperty],
                }
            };
        }
    }
}
