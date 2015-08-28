﻿// -----------------------------------------------------------------------
// <copyright file="ControlExtensions.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System.Linq;
    using Perspex.LogicalTree;
    using Perspex.Styling;

    /// <summary>
    /// Adds common functionality to <see cref="IControl"/>.
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// Tries to being the control into view.
        /// </summary>
        /// <param name="control">The control.</param>
        public static void BringIntoView(this IControl control)
        {
            control.BringIntoView(new Rect(control.Bounds.Size));
        }

        /// <summary>
        /// Tries to being the control into view.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="rect">The area of the control to being into view.</param>
        public static void BringIntoView(this IControl control, Rect rect)
        {
            var ev = new RequestBringIntoViewEventArgs
            {
                RoutedEvent = Control.RequestBringIntoViewEvent,
                TargetObject = control,
                TargetRect = rect,
            };

            control.RaiseEvent(ev);
        }

        /// <summary>
        /// Finds the named control in the specified control.
        /// </summary>
        /// <typeparam name="T">The type of the control to find.</typeparam>
        /// <param name="control">The control.</param>
        /// <param name="name">The name of the control to find.</param>
        /// <returns>The control or null if not found.</returns>
        public static T FindControl<T>(this IControl control, string name) where T : IControl
        {
            return control.GetLogicalDescendents()
                .OfType<T>()
                .FirstOrDefault(x => x.Name == name);
        }
    }
}
