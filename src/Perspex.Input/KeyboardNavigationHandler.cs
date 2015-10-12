// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Input.Navigation;

namespace Perspex.Input
{
    /// <summary>
    /// Handles keyboard navigation for a window.
    /// </summary>
    public class KeyboardNavigationHandler : IKeyboardNavigationHandler
    {
        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot _owner;

        /// <summary>
        /// Sets the owner of the keyboard navigation handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            Contract.Requires<ArgumentNullException>(owner != null);

            if (_owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            _owner = owner;

            _owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        /// <summary>
        /// Gets the next control in the specified navigation direction.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>
        /// The next element in the specified direction, or null if <paramref name="element"/>
        /// was the last in therequested direction.
        /// </returns>
        public static IInputElement GetNext(
            IInputElement element,
            FocusNavigationDirection direction)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            if (direction == FocusNavigationDirection.Next || direction == FocusNavigationDirection.Previous)
            {
                return TabNavigation.GetNextInTabOrder(element, direction);
            }
            else
            {
                return DirectionalNavigation.GetNext(element, direction);
            }
        }

        /// <summary>
        /// Moves the focus in the specified direction.
        /// </summary>
        /// <param name="element">The current element.</param>
        /// <param name="direction">The direction to move.</param>
        /// <param name="modifiers">Any input modifiers active at the time of focus.</param>
        public void Move(
            IInputElement element, 
            FocusNavigationDirection direction,
            InputModifiers modifiers = InputModifiers.None)
        {
            Contract.Requires<ArgumentNullException>(element != null);

            var next = GetNext(element, direction);

            if (next != null)
            {
                var method = direction == FocusNavigationDirection.Next ||
                             direction == FocusNavigationDirection.Previous ?
                             NavigationMethod.Tab : NavigationMethod.Directional;
                FocusManager.Instance.Focus(next, method, modifiers);
            }
        }

        /// <summary>
        /// Handles the Tab key being pressed in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            var current = FocusManager.Instance.Current;

            if (current != null)
            {
                FocusNavigationDirection? direction = null;

                switch (e.Key)
                {
                    case Key.Tab:
                        direction = (e.Modifiers & InputModifiers.Shift) == 0 ?
                            FocusNavigationDirection.Next : FocusNavigationDirection.Previous;
                        break;
                    case Key.Up:
                        direction = FocusNavigationDirection.Up;
                        break;
                    case Key.Down:
                        direction = FocusNavigationDirection.Down;
                        break;
                    case Key.Left:
                        direction = FocusNavigationDirection.Left;
                        break;
                    case Key.Right:
                        direction = FocusNavigationDirection.Right;
                        break;
                }

                if (direction.HasValue)
                {
                    Move(current, direction.Value, e.Modifiers);
                    e.Handled = true;
                }
            }
        }
    }
}
