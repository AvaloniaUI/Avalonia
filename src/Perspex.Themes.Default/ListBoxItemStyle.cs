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
    /// The default style for the <see cref="ListBoxItem"/> control.
    /// </summary>
    public class ListBoxItemStyle : Styles
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListBoxItemStyle"/> class.
        /// </summary>
        public ListBoxItemStyle()
        {
            AddRange(new[]
            {
                new Style(x => x.OfType<ListBoxItem>())
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.TemplateProperty, new FuncControlTemplate<ListBoxItem>(Template)),
                    },
                },
                new Style(x => x.OfType<ListBoxItem>().Class(":selected").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xfff0f0f0)),
                    },
                },
                new Style(x => x.OfType<ListBoxItem>().Class(":selected").Class(":focus").Template().Name("border"))
                {
                    Setters = new[]
                    {
                        new Setter(TemplatedControl.BackgroundProperty, new SolidColorBrush(0xffd0d0d0)),
                    },
                },
            });
        }

        /// <summary>
        /// The default template for a <see cref="ListBoxItem"/> control.
        /// </summary>
        /// <param name="control">The control being styled.</param>
        /// <returns>The root of the instantiated template.</returns>
        public static Control Template(ListBoxItem control)
        {
            return new Border
            {
                Name = "border",
                [~Border.BackgroundProperty] = control[~TemplatedControl.BackgroundProperty],
                [~Border.BorderBrushProperty] = control[~TemplatedControl.BorderBrushProperty],
                [~Border.BorderThicknessProperty] = control[~TemplatedControl.BorderThicknessProperty],
                Child = new ContentPresenter
                {
                    [~ContentPresenter.ContentProperty] = control[~ContentControl.ContentProperty],
                },
            };
        }
    }
}
