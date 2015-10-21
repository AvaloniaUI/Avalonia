// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Collections;
using Perspex.Metadata;

namespace Perspex.Controls
{
    /// <summary>
    /// Base class for controls which decorate a single child control.
    /// </summary>
    public class Decorator : Control
    {
        /// <summary>
        /// Defines the <see cref="Child"/> property.
        /// </summary>
        public static readonly PerspexProperty<Control> ChildProperty =
            PerspexProperty.Register<Decorator, Control>(nameof(Child));

        /// <summary>
        /// Defines the <see cref="Padding"/> property.
        /// </summary>
        public static readonly PerspexProperty<Thickness> PaddingProperty =
            PerspexProperty.Register<Decorator, Thickness>(nameof(Padding));

        /// <summary>
        /// Initializes static members of the <see cref="Decorator"/> class.
        /// </summary>
        static Decorator()
        {
            ChildProperty.Changed.AddClassHandler<Decorator>(x => x.ChildChanged);
        }

        /// <summary>
        /// Gets or sets the decorated control.
        /// </summary>
        [Content]
        public Control Child
        {
            get { return GetValue(ChildProperty); }
            set { SetValue(ChildProperty, value); }
        }

        /// <summary>
        /// Gets or sets the padding to place around the <see cref="Child"/> control.
        /// </summary>
        public Thickness Padding
        {
            get { return GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        /// <inheritdoc/>
        protected override Size MeasureOverride(Size availableSize)
        {
            var content = Child;
            var padding = Padding;

            if (content != null)
            {
                content.Measure(availableSize.Deflate(padding));
                return content.DesiredSize.Inflate(padding);
            }
            else
            {
                return new Size(padding.Left + padding.Right, padding.Bottom + padding.Top);
            }
        }

        /// <inheritdoc/>
        protected override Size ArrangeOverride(Size finalSize)
        {
            Control content = Child;

            if (content != null)
            {
                content.Arrange(new Rect(finalSize).Deflate(Padding));
            }

            return finalSize;
        }

        /// <summary>
        /// Called when the <see cref="Child"/> property changes.
        /// </summary>
        /// <param name="e">The event args.</param>
        private void ChildChanged(PerspexPropertyChangedEventArgs e)
        {
            var oldChild = (Control)e.OldValue;
            var newChild = (Control)e.NewValue;

            if (oldChild != null)
            {
                ((ISetLogicalParent)oldChild).SetParent(null);
                LogicalChildren.Clear();
                RemoveVisualChild(oldChild);
            }

            if (newChild != null)
            {
                AddVisualChild(newChild);
                LogicalChildren.Add(newChild);
                ((ISetLogicalParent)newChild).SetParent(this);
            }
        }
    }
}
