// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Interactivity;
using Perspex.Controls.Primitives;

namespace Perspex.Controls.Mixins
{
    /// <summary>
    /// Adds selectable functionality to control classes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="SelectableMixin"/> adds behavior to a control which can be
    /// selected. It adds the following behavior:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Raises an <see cref="SelectingItemsControl.IsSelectedChangedEvent"/> when the value if
    /// the IsSelected property changes.
    /// </item>
    /// <item>
    /// Adds a 'selected' class to selected controls.
    /// </item>
    /// <item>
    /// Requests that the control is scrolled into view when focused.
    /// </item>
    /// </list>
    /// <para>
    /// Mixins apply themselves to classes and not instances, and as such should be created in
    /// a static constructor.
    /// </para>
    /// </remarks>
    public static class SelectableMixin
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SelectableMixin"/> class.
        /// </summary>
        /// <typeparam name="TControl">The control type.</typeparam>
        /// <param name="isSelected">The IsSelected property.</param>
        public static void Attach<TControl>(PerspexProperty<bool> isSelected)
            where TControl : class, IControl
        {
            Contract.Requires<ArgumentNullException>(isSelected != null);

            isSelected.Changed.Subscribe(x =>
            {
                var sender = x.Sender as TControl;

                if (sender != null)
                {
                    if ((bool)x.NewValue)
                    {
                        ((IPseudoClasses)sender.Classes).Add(":selected");

                        if (((IVisual)sender).IsAttachedToVisualTree)
                        {
                            sender.BringIntoView();
                        }
                    }
                    else
                    {
                        ((IPseudoClasses)sender.Classes).Remove(":selected");
                    }

                    sender.RaiseEvent(new RoutedEventArgs
                    {
                        RoutedEvent = SelectingItemsControl.IsSelectedChangedEvent
                    });
                }
            });
        }
    }
}