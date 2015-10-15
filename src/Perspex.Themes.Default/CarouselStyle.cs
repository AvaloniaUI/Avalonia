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
    /// The default style for the <see cref="Carousel"/> control.
    /// </summary>
    public class CarouselStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CarouselStyle"/> class.
        /// </summary>
        public CarouselStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<Carousel>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new ControlTemplate<Carousel>(Template)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for the <see cref="Carousel"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(Carousel control)
        {
            return new CarouselPresenter
            {
                Name = "itemsPresenter",
                MemberSelector = control.MemberSelector,
                [~ItemsPresenter.ItemsProperty] = control[~ItemsControl.ItemsProperty],
                [~ItemsPresenter.ItemsPanelProperty] = control[~ItemsControl.ItemsPanelProperty],
                [~CarouselPresenter.SelectedIndexProperty] = control[~SelectingItemsControl.SelectedIndexProperty],
                [~CarouselPresenter.TransitionProperty] = control[~Carousel.TransitionProperty],
            };
        }
    }
}
