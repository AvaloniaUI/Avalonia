// -----------------------------------------------------------------------
// <copyright file="AccessKeyHandler.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

using Perspex.Interactivity;
using System;

namespace Perspex.Input
{
    /// <summary>
    /// Handles access keys for a window.
    /// </summary>
    public class AccessKeyHandler : IAccessKeyHandler
    {
        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot owner;

        /// <summary>
        /// Whether access keys are currently being shown;
        /// </summary>
        private bool showingAccessKeys;

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
            if (this.owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            this.owner = owner;

            this.owner.AddHandler(InputElement.KeyDownEvent, this.OnPreviewKeyDown, RoutingStrategies.Tunnel);
            this.owner.AddHandler(InputElement.KeyUpEvent, this.OnPreviewKeyUp, RoutingStrategies.Tunnel);
            this.owner.AddHandler(InputElement.PointerPressedEvent, this.OnPreviewPointerPressed, RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Handles the Alt/F10 keys being pressed in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt || e.Key == Key.F10)
            {
                this.owner.ShowAccessKeys = this.showingAccessKeys = true;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles the Alt/F10 keys being released in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftAlt || e.Key == Key.F10)
            {
                if (this.showingAccessKeys && this.MainMenu != null)
                {
                    this.MainMenu.OpenMenu();
                    e.Handled = true;
                }
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
    }
}
