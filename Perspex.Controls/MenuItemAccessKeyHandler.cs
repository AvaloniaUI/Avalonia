// -----------------------------------------------------------------------
// <copyright file="MenuItemAccessKeyHandler.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Controls
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.Input;
    using Perspex.Interactivity;

    /// <summary>
    /// Handles access keys within a <see cref="MenuItem"/>
    /// </summary>
    public class MenuItemAccessKeyHandler : IAccessKeyHandler
    {
        /// <summary>
        /// The registered access keys.
        /// </summary>
        private List<Tuple<string, IInputElement>> registered = new List<Tuple<string, IInputElement>>();

        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot owner;

        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        /// <remarks>
        /// This property is ignored as a menu item cannot have a main menu.
        /// </remarks>
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

            this.owner.AddHandler(InputElement.KeyDownEvent, this.OnKeyDown);
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
        /// Handles a key being pressed in the menu.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Text))
            {
                var text = e.Text.ToUpper();
                var focus = this.registered
                    .Where(x => x.Item1 == text && x.Item2.IsEffectivelyVisible)
                    .FirstOrDefault()?.Item2;

                if (focus != null)
                {
                    focus.RaiseEvent(new RoutedEventArgs(AccessKeyHandler.AccessKeyPressedEvent));
                }

                e.Handled = true;
            }
        }
    }
}
