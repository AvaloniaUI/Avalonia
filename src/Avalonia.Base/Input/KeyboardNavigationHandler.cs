using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Input.Navigation;
using Avalonia.Input;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Handles keyboard navigation for a window.
    /// </summary>
    [Unstable]
    public sealed class KeyboardNavigationHandler : IKeyboardNavigationHandler
    {
        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot? _owner;
        
        /// <summary>
        /// Sets the owner of the keyboard navigation handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        [PrivateApi]
        public void SetOwner(IInputRoot owner)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException($"{nameof(KeyboardNavigationHandler)} owner has already been set.");
            }

            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        /// <summary>
        /// Gets the next control in the specified navigation direction.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="direction">The navigation direction.</param>
        /// <returns>
        /// The next element in the specified direction, or null if <paramref name="element"/>
        /// was the last in the requested direction.
        /// </returns>
        public static IInputElement? GetNext(
            IInputElement element,
            NavigationDirection direction)
        {
            element = element ?? throw new ArgumentNullException(nameof(element));
            return GetNextPrivate(element, null, direction, null);
        }

        private static IInputElement? GetNextPrivate(
            IInputElement? element,
            IInputRoot? owner,
            NavigationDirection direction,
            KeyDeviceType? keyDeviceType)
        {
            var elementOrOwner = element ?? owner ?? throw new ArgumentNullException(nameof(owner));

            // If there's a custom keyboard navigation handler as an ancestor, use that.
            var custom = (element as Visual)?.FindAncestorOfType<ICustomKeyboardNavigation>(true);
            if (custom is not null && HandlePreCustomNavigation(custom, elementOrOwner, direction, out var ce))
                return ce;

            IInputElement? result;
            if (direction is NavigationDirection.Next)
            {
                result = TabNavigation.GetNextTab(elementOrOwner, false);
            }
            else if (direction is NavigationDirection.Previous)
            {
                result = TabNavigation.GetPrevTab(elementOrOwner, null, false);
            }
            else if (direction is NavigationDirection.Up or NavigationDirection.Down
                     or NavigationDirection.Left or NavigationDirection.Right)
            {
                // HACK: a window should always have some element focused,
                // it seems to be a difference between UWP and Avalonia focus manager implementations.
                result = element is null
                    ? TabNavigation.GetNextTab(elementOrOwner, true)
                    : XYFocus.TryDirectionalFocus(direction, element, owner, null, keyDeviceType);
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(direction), direction, null);
            }

            // If there wasn't a custom navigation handler as an ancestor of the current element,
            // but there is one as an ancestor of the new element, use that.
            if (custom is null && HandlePostCustomNavigation(elementOrOwner, result, direction, out ce))
                return ce;

            return result;
        }

        /// <summary>
        /// Moves the focus in the specified direction.
        /// </summary>
        /// <param name="element">The current element.</param>
        /// <param name="direction">The direction to move.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        public void Move(
            IInputElement? element,
            NavigationDirection direction,
            KeyModifiers keyModifiers = KeyModifiers.None)
        {
            MovePrivate(element, direction, keyModifiers, null);
        }

        // TODO12: remove MovePrivate, and make Move return boolean. Or even remove whole KeyboardNavigationHandler.
        private bool MovePrivate(IInputElement? element, NavigationDirection direction, KeyModifiers keyModifiers, KeyDeviceType? deviceType)
        {
            var next = GetNextPrivate(element, _owner, direction, deviceType);

            if (next != null)
            {
                var method = direction == NavigationDirection.Next ||
                             direction == NavigationDirection.Previous ?
                    NavigationMethod.Tab : NavigationMethod.Directional;
                return next.Focus(method, keyModifiers);
            }

            return false;
        }

        /// <summary>
        /// Handles the Tab key being pressed in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        void OnKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab)
            {
                var current = FocusManager.GetFocusManager(e.Source as IInputElement)?.GetFocusedElement();
                var direction = (e.KeyModifiers & KeyModifiers.Shift) == 0 ?
                    NavigationDirection.Next : NavigationDirection.Previous;
                e.Handled = MovePrivate(current, direction, e.KeyModifiers, e.KeyDeviceType);
            }
            else if (e.Key is Key.Left or Key.Right or Key.Up or Key.Down)
            {
                var current = FocusManager.GetFocusManager(e.Source as IInputElement)?.GetFocusedElement();
                var direction = e.Key switch
                {
                    Key.Left => NavigationDirection.Left,
                    Key.Right => NavigationDirection.Right,
                    Key.Up => NavigationDirection.Up,
                    Key.Down => NavigationDirection.Down,
                    _ => throw new ArgumentOutOfRangeException()
                };
                e.Handled = MovePrivate(current, direction, e.KeyModifiers, e.KeyDeviceType);
            }
        }

        private static bool HandlePreCustomNavigation(
            ICustomKeyboardNavigation customHandler,
            IInputElement element,
            NavigationDirection direction,
            [NotNullWhen(true)] out IInputElement? result)
        {
            var (handled, next) = customHandler.GetNext(element, direction);

            if (handled)
            {
                if (next is not null)
                {
                    result = next;
                    return true;
                }

                var r = direction switch
                {
                    NavigationDirection.Next => TabNavigation.GetNextTabOutside(customHandler),
                    NavigationDirection.Previous => TabNavigation.GetPrevTabOutside(customHandler),
                    _ => null
                };

                if (r is not null)
                {
                    result = r;
                    return true;
                }
            }

            result = null;
            return false;
        }

        private static bool HandlePostCustomNavigation(
            IInputElement element,
            IInputElement? newElement,
            NavigationDirection direction,
            [NotNullWhen(true)] out IInputElement? result)
        {
            if (newElement is Visual v)
            {
                var customHandler = v.FindAncestorOfType<ICustomKeyboardNavigation>(true);

                if (customHandler is object)
                {
                    var (handled, next) = customHandler.GetNext(element, direction);

                    if (handled && next is object)
                    {
                        result = next;
                        return true;
                    }
                }
            }

            result = null;
            return false;
        }
    }
}
