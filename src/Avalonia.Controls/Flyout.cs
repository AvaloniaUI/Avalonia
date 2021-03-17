using System;
using System.Collections.Generic;
using System.Text;

using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public class Flyout : FlyoutBase
    {
        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<IControl> ContentProperty =
            AvaloniaProperty.Register<ContentControl, IControl>(nameof(Content));

        /// <summary>
        /// Gets or sets the content to display.
        /// </summary>
        [Content]
        public IControl Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        protected override Control CreatePresenter()
        {
            return new Border
            {
                Padding = new Thickness(20),
                Background = new SolidColorBrush(Colors.Blue),
                MinHeight = 100,
                MinWidth = 100,
                Child = Content
            };
        }
    }
}
