using System;
using Avalonia.Interactivity;

namespace Avalonia.Input
{
    /// <summary>
    /// Handles keyboard navigation for a window.
    /// </summary>
    public class KeyboardNavigationHandler : IKeyboardNavigationHandler
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
        public void SetOwner(IInputRoot owner)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown);
        }

        /// <summary>
        /// Handles the Tab key being pressed in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object? sender, KeyEventArgs e)
        {
            var current = FocusManager.GetFocusedElement();

            if (current != null && e.Key == Key.Tab)
            {
                var direction = (e.KeyModifiers & KeyModifiers.Shift) == 0 ?
                    NavigationDirection.Next : NavigationDirection.Previous;

                var result = FocusManager.FindNextElement(direction);

                if (result != null)
                {
                    var focusManager = FocusManager.GetFocusManagerFromElement(current);

                    focusManager.SetFocusedElement(result, direction,
                        state: FocusState.Keyboard,
                        keyModifiers: e.KeyModifiers);
                    e.Handled = true;
                }

                if (!e.Handled)
                {
                    RaiseNoFocusCandidateFound(current ?? _owner!);
                    e.Handled = true;
                }
            }
        }

        private void RaiseNoFocusCandidateFound(IInteractive source)
        {
            source.RaiseEvent(new RoutedEventArgs(InputElement.NoFocusCandidateFoundEvent, source));
        }

    }
}
