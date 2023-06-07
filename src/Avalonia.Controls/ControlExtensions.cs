using System;
using Avalonia.Reactive;

namespace Avalonia.Controls
{
    /// <summary>
    /// Adds common functionality to <see cref="Control"/>.
    /// </summary>
    public static class ControlExtensions
    {
        /// <summary>
        /// Tries to bring the control into view.
        /// </summary>
        /// <param name="control">The control.</param>
        public static void BringIntoView(this Control control)
        {
            _ = control ?? throw new ArgumentNullException(nameof(control));

            control.BringIntoView(new Rect(control.Bounds.Size));
        }

        /// <summary>
        /// Tries to bring the control into view.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <param name="rect">The area of the control to being into view.</param>
        public static void BringIntoView(this Control control, Rect rect)
        {
            _ = control ?? throw new ArgumentNullException(nameof(control));

            if (control.IsEffectivelyVisible)
            {
                var ev = new RequestBringIntoViewEventArgs
                {
                    RoutedEvent = Control.RequestBringIntoViewEvent,
                    TargetObject = control,
                    TargetRect = rect,
                };

                control.RaiseEvent(ev);
            }
        }

        /// <summary>
        /// Finds the named control in the scope of the specified control.
        /// </summary>
        /// <typeparam name="T">The type of the control to find.</typeparam>
        /// <param name="control">The control to look in.</param>
        /// <param name="name">The name of the control to find.</param>
        /// <returns>The control or null if not found.</returns>
        public static T? FindControl<T>(this Control control, string name) where T : Control
        {
            _ = control ?? throw new ArgumentNullException(nameof(control));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var nameScope = control.FindNameScope();

            if (nameScope == null)
            {
                throw new InvalidOperationException("Could not find parent name scope.");
            }

            return nameScope.Find<T>(name);
        }

        /// <summary>
        /// Finds the named control in the scope of the specified control and throws if not found.
        /// </summary>
        /// <typeparam name="T">The type of the control to find.</typeparam>
        /// <param name="control">The control to look in.</param>
        /// <param name="name">The name of the control to find.</param>
        /// <returns>The control.</returns>
        public static T GetControl<T>(this Control control, string name) where T : Control
        {
            _ = control ?? throw new ArgumentNullException(nameof(control));
            _ = name ?? throw new ArgumentNullException(nameof(name));

            var nameScope = control.FindNameScope();

            if (nameScope == null)
            {
                throw new InvalidOperationException("Could not find parent name scope.");
            }

            return nameScope.Find<T>(name) ??
                throw new ArgumentException($"Could not find control named '{name}'.");
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
            _ = classes ?? throw new ArgumentNullException(nameof(classes));

            return trigger.Subscribe(x => classes.Set(name, x));
        }
    }
}
