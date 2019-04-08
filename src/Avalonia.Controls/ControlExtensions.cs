// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Linq;
using Avalonia.Data;
using Avalonia.LogicalTree;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Adds common functionality to <see cref="IControl"/>.
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// Tries to bring the control into view.
        /// </summary>
        /// <param name="control">The control.</param>
        public static void BringIntoView(this IControl control)
        {
            Contract.Requires<ArgumentNullException>(control != null);

            control.BringIntoView(new Rect(control.Bounds.Size));
        }

        /// <summary>
        /// Tries to bring the control into view.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="rect">The area of the control to being into view.</param>
        public static void BringIntoView(this IControl control, Rect rect)
        {
            Contract.Requires<ArgumentNullException>(control != null);

            var ev = new RequestBringIntoViewEventArgs
            {
                RoutedEvent = Control.RequestBringIntoViewEvent,
                TargetObject = control,
                TargetRect = rect,
            };

            control.RaiseEvent(ev);
        }

        /// <summary>
        /// Finds the named control in the scope of the specified control.
        /// </summary>
        /// <typeparam name="T">The type of the control to find.</typeparam>
        /// <param name="control">The control to look in.</param>
        /// <param name="name">The name of the control to find.</param>
        /// <returns>The control or null if not found.</returns>
        public static T FindControl<T>(this IControl control, string name) where T : class, IControl
        {
            Contract.Requires<ArgumentNullException>(control != null);
            Contract.Requires<ArgumentNullException>(name != null);

            var nameScope = control.FindNameScope();

            if (nameScope == null)
            {
                throw new InvalidOperationException("Could not find parent name scope.");
            }

            return nameScope.Find<T>(name);
        }

        /// <summary>
        /// Adds or removes a pseudoclass depending on a boolean value.
        /// </summary>
        /// <param name="classes">The pseudoclasses collection.</param>
        /// <param name="name">The name of the pseudoclass to set.</param>
        /// <param name="value">True to add the pseudoclass or false to remove.</param>
        public static void Set(this IPseudoClasses classes, string name, bool value)
        {
            Contract.Requires<ArgumentNullException>(classes != null);

            if (value)
            {
                classes.Add(name);
            }
            else
            {
                classes.Remove(name);
            }
        }

        /// <summary>
        /// Sets a pseudoclass depending on an observable trigger.
        /// </summary>
        /// <param name="classes">The pseudoclasses collection.</param>
        /// <param name="name">The name of the pseudoclass to set.</param>
        /// <param name="trigger">The trigger: true adds the pseudoclass, false removes.</param>
        /// <returns>A disposable used to cancel the subscription.</returns>
        public static IDisposable Set(this IPseudoClasses classes, string name, IObservable<bool> trigger)
        {
            Contract.Requires<ArgumentNullException>(classes != null);

            return trigger.Subscribe(x => classes.Set(name, x));
        }
    }
}
