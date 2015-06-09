// -----------------------------------------------------------------------
// <copyright file="FocusManager.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Input
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Perspex.VisualTree;
    using Splat;

    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    public class FocusManager : IFocusManager
    {
        /// <summary>
        /// The focus scopes in which the focus is currently defined.
        /// </summary>
        private Dictionary<IFocusScope, IInputElement> focusScopes =
            new Dictionary<IFocusScope, IInputElement>();

        /// <summary>
        /// Gets the instance of the <see cref="IFocusManager"/>.
        /// </summary>
        public static IFocusManager Instance
        {
            get { return Locator.Current.GetService<IFocusManager>(); }
        }

        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        public IInputElement Current
        {
            get { return KeyboardDevice.Instance.FocusedElement; }
        }

        /// <summary>
        /// Gets the current focus scope.
        /// </summary>
        public IFocusScope Scope
        {
            get;
            private set;
        }

        /// <summary>
        /// Focuses a control.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        /// <param name="keyboardNavigated">
        /// Whether the control was focused by a keypress (e.g. the Tab key).
        /// </param>
        public void Focus(IInputElement control, bool keyboardNavigated = false)
        {
            if (control != null)
            {
                var scope = control.GetSelfAndVisualAncestors()
                    .OfType<IFocusScope>()
                    .FirstOrDefault();

                if (scope != null)
                {
                    this.Scope = scope;
                    this.SetFocusedElement(scope, control, keyboardNavigated);
                }
            }
            else
            {
                this.SetFocusedElement(this.Scope, null);
            }
        }

        /// <summary>
        /// Sets the currently focused element in the specified scope.
        /// </summary>
        /// <param name="scope">The focus scope.</param>
        /// <param name="element">The element to focus. May be null.</param>
        /// <param name="keyboardNavigated">
        /// Whether the control was focused by a keypress (e.g. the Tab key).
        /// </param>
        /// <remarks>
        /// If the specified scope is the current <see cref="Scope"/> then the keyboard focus
        /// will change.
        /// </remarks>
        public void SetFocusedElement(
            IFocusScope scope,
            IInputElement element,
            bool keyboardNavigated = false)
        {
            Contract.Requires<ArgumentNullException>(scope != null);

            this.focusScopes[scope] = element;

            if (this.Scope == scope)
            {
                KeyboardDevice.Instance.SetFocusedElement(element, keyboardNavigated);
            }
        }

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        public void SetFocusScope(IFocusScope scope)
        {
            Contract.Requires<ArgumentNullException>(scope != null);

            IInputElement e;

            if (!this.focusScopes.TryGetValue(scope, out e))
            {
                // TODO: Make this do something useful, i.e. select the first focusable
                // control, select a control that the user has specified to have default
                // focus etc.
                e = scope as IInputElement;
                this.focusScopes.Add(scope, e);
            }

            this.Scope = scope;
            this.Focus(e);
        }
    }
}
