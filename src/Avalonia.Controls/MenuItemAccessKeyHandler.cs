using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// Handles access keys within a <see cref="MenuItem"/>
    /// </summary>
    internal class MenuItemAccessKeyHandler : IAccessKeyHandler
    {
        private IInputElement? _focusElement;

        /// <summary>
        /// The registered access keys.
        /// </summary>
        private readonly List<(string AccessKey, IInputElement Element)> _registered = new();

        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot? _owner;

        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        /// <remarks>
        /// This property is ignored as a menu item cannot have a main menu.
        /// </remarks>
        public IMainMenu? MainMenu { get; set; }

        /// <summary>
        /// Sets the owner of the access key handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            _ = owner ?? throw new ArgumentNullException(nameof(owner));

            if (_owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            _owner = owner;

            _owner.AddHandler(InputElement.TextInputEvent, OnTextInput);
            // _owner.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Registers an input element to be associated with an access key.
        /// </summary>
        /// <param name="accessKey">The access key.</param>
        /// <param name="element">The input element.</param>
        public void Register(char accessKey, IInputElement element)
        {
            var existing = _registered.FirstOrDefault(x => x.Item2 == element);

            if (existing != default)
            {
                _registered.Remove(existing);
            }

            _registered.Add((accessKey.ToString().ToUpperInvariant(), element));
        }

        /// <summary>
        /// Unregisters the access keys associated with the input element.
        /// </summary>
        /// <param name="element">The input element.</param>
        public void Unregister(IInputElement element)
        {
            foreach (var i in _registered.Where(x => x.Item2 == element).ToList())
            {
                _registered.Remove(i);
            }
        }

        /// <summary>
        /// Handles a key being pressed in the menu.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnTextInput(object? sender, TextInputEventArgs e)
        {
            // only handle accelerators, when access keys are visible
            if (!(_owner?.ShowAccessKeys ?? false))
                return;

            // select menu items matching the access key which are visible and enabled.
            var matches = _registered
                .Where(x => string.Equals(x.AccessKey, e.Text, StringComparison.OrdinalIgnoreCase)
                            && x.Element is
                            {
                                IsEffectivelyVisible: true,
                                IsEffectivelyEnabled: true
                            })
                .Select(x => x.Element);

            var count = matches.Count();
            if (count == 1) // If there is a match, raise the AccessKeyPressed event on it.
            {
                // reset the currently selected focus element
                _focusElement = null;
                var element = matches.FirstOrDefault();
                element?.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));
                e.Handled = true;
            }
            else if (count > 1) // If there are multiple elements, cycle focus through them.
            {
                _focusElement = _focusElement == null ?
                    (matches.FirstOrDefault() as Visual)?.Parent as IInputElement :
                    AccessKeyHandler.GetNextElementToFocus(matches, _focusElement);

                _focusElement?.Focus(NavigationMethod.Tab, KeyModifiers.Alt);
                e.Handled = true;
            }
        }
    }
}
