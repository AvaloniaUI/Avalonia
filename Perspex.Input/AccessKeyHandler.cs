// -----------------------------------------------------------------------
// <copyright file="AccessKeyHandler.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Interactivity;
    using Perspex.VisualTree;

    /// <summary>
    /// Handles access keys for a window.
    /// </summary>
    public class AccessKeyHandler : IAccessKeyHandler
    {
        /// <summary>
        /// Defines the AccessKeyPressed attached event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> AccessKeyPressedEvent =
            RoutedEvent.Register<RoutedEventArgs>(
                "AccessKeyPressed",
                RoutingStrategies.Bubble,
                typeof(AccessKeyHandler));

        /// <summary>
        /// The registered access keys.
        /// </summary>
        private List<Tuple<string, IInputElement>> registered = new List<Tuple<string, IInputElement>>();

        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot owner;

        /// <summary>
        /// Whether access keys are currently being shown;
        /// </summary>
        private bool showingAccessKeys;

        /// <summary>
        /// Whether to ignore the Alt KeyUp event.
        /// </summary>
        private bool ignoreAltUp;

        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        public IMainMenu MainMenu { get; set; }

        /// <summary>
        /// Sets the owner of the access key handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            Contract.Requires<ArgumentNullException>(owner != null);

            if (this.owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            this.owner = owner;

            this.owner.AddHandler(InputElement.KeyDownEvent, this.OnPreviewKeyDown, RoutingStrategies.Tunnel);
            this.owner.AddHandler(InputElement.KeyDownEvent, this.OnKeyDown, RoutingStrategies.Bubble);
            this.owner.AddHandler(InputElement.KeyUpEvent, this.OnPreviewKeyUp, RoutingStrategies.Tunnel);
            this.owner.AddHandler(InputElement.PointerPressedEvent, this.OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Registers an input element to be associated with an access key.
        /// </summary>
        /// <param name="accessKey">The access key.</param>
        /// <param name="element">The input element.</param>
        public void Register(char accessKey, IInputElement element)
        {
            var existing = this.registered.FirstOrDefault(x => x.Item2 == element);

            if (existing != null)
            {
                this.registered.Remove(existing);
            }

            this.registered.Add(Tuple.Create(accessKey.ToString().ToUpper(), element));
        }

        /// <summary>
        /// Unregisters the access keys associated with the input element.
        /// </summary>
        /// <param name="element">The input element.</param>
        public void Unregister(IInputElement element)
        {
            foreach (var i in this.registered.Where(x => x.Item2 == element).ToList())
            {
                this.registered.Remove(i);
            }
        }

        /// <summary>
        /// Called when a key is pressed in the owner window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt)
            {
                if (this.MainMenu == null || !this.MainMenu.IsOpen)
                {
                    // When Alt is pressed without a main menu, or with a closed main menu, show
                    // access key markers in the window (i.e. "_File").
                    this.owner.ShowAccessKeys = this.showingAccessKeys = true;
                }
                else
                {
                    // If the Alt key is pressed and the main menu is open, close the main menu.
                    this.CloseMenu();
                    this.ignoreAltUp = true;
                }

                // We always handle the Alt key.
                e.Handled = true;
            }
        }

        /// <summary>
        /// Called when a key is pressed in the owner window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            bool menuIsOpen = this.MainMenu?.IsOpen == true;

            if (e.Key == Key.Escape && menuIsOpen)
            {
                // When the Escape key is pressed with the main menu open, close it.
                this.CloseMenu();
                e.Handled = true;
            }
            else if ((KeyboardDevice.Instance.Modifiers & ModifierKeys.Alt) != 0 || menuIsOpen)
            {
                // If any other key is pressed with the Alt key held down, or the main menu is open,
                // find all controls who have registered that access key.
                var text = e.Text.ToUpper();
                var matches = this.registered
                    .Where(x => x.Item1 == text && x.Item2.IsEffectivelyVisible)
                    .Select(x => x.Item2);

                // If the menu is open, only match controls in the menu's visual tree.
                if (menuIsOpen)
                {
                    matches = matches.Where(x => this.MainMenu.IsVisualParentOf(x));
                }

                var match = matches.FirstOrDefault();

                // If there was a match, raise the AccessKeyPressed event on it.
                if (match != null)
                {
                    match.RaiseEvent(new RoutedEventArgs(AccessKeyPressedEvent));
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Handles the Alt/F10 keys being released in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftAlt:
                    if (this.ignoreAltUp)
                    {
                        this.ignoreAltUp = false;
                    }
                    else if (this.showingAccessKeys && this.MainMenu != null)
                    {
                        this.MainMenu.Open();
                        e.Handled = true;
                    }

                    break;

                case Key.F10:
                    this.owner.ShowAccessKeys = this.showingAccessKeys = true;
                    this.MainMenu.Open();
                    e.Handled = true;
                    break;
            }
        }

        /// <summary>
        /// Handles pointer presses in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewPointerPressed(object sender, PointerEventArgs e)
        {
            if (this.showingAccessKeys)
            {
                this.owner.ShowAccessKeys = false;
            }
        }

        /// <summary>
        /// Closes the <see cref="MainMenu"/> and performs other bookeeping.
        /// </summary>
        private void CloseMenu()
        {
            this.MainMenu.Close();
            this.owner.ShowAccessKeys = this.showingAccessKeys = false;
        }
    }
}
