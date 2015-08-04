// -----------------------------------------------------------------------
// <copyright file="SelectableMixin.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls.Mixins
{
    using System;
    using Perspex.Interactivity;
    using Primitives;

    /// <summary>
    /// Adds selectable functionality to control classes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <see cref="SelectableMixin{TControl}"/> adds behavior to a control which can be
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
        /// Initializes a new instance of the <see cref="SelectableMixin{TControl}"/> class.
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
                        sender.Classes.Add("selected");

                        if (((IVisual)sender).IsAttachedToVisualTree)
                        {
                            sender.BringIntoView();
                        }
                    }
                    else
                    {
                        sender.Classes.Remove("selected");
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