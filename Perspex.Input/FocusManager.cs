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
    using Perspex.Interactivity;
    using Perspex.VisualTree;
    using Splat;

    public class FocusManager : IFocusManager
    {
        private Dictionary<IFocusScope, IInputElement> focusScopes = 
            new Dictionary<IFocusScope, IInputElement>();

        public static IFocusManager Instance
        {
            get { return Locator.Current.GetService<IFocusManager>(); }
        }

        public IInputElement Current
        {
            get;
            private set;
        }

        public IFocusScope Scope
        {
            get;
            private set;
        }

        /// <summary>
        /// Focuses a control.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        public void Focus(IInputElement control)
        {
            Contract.Requires<ArgumentNullException>(control != null);

            var current = this.Current as IInteractive;
            var next = control as IInteractive;
            var scope = control.GetSelfAndVisualAncestors()
                .OfType<IFocusScope>()
                .FirstOrDefault();

            if (scope != null && control != current)
            {
                this.focusScopes[scope] = control;

                if (current != null)
                {
                    current.RaiseEvent(new RoutedEventArgs
                    {
                        RoutedEvent = InputElement.LostFocusEvent,
                        Source = current,
                        OriginalSource = current,
                    });
                }

                this.Current = control;

                IKeyboardDevice keyboard = Locator.Current.GetService<IKeyboardDevice>();

                if (keyboard != null)
                {
                    keyboard.FocusedElement = control;
                }

                if (next != null)
                {
                    next.RaiseEvent(new RoutedEventArgs
                    {
                        RoutedEvent = InputElement.GotFocusEvent,
                        Source = next,
                        OriginalSource = next,
                    });
                }
            }
        }

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        /// <remarks>
        /// This should not be called by client code. It is called by an <see cref="IFocusScope"/>
        /// when it activates, e.g. when a Window is activated.
        /// </remarks>
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
