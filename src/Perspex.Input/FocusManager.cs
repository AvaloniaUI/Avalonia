﻿// -----------------------------------------------------------------------
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
        /// Initializes a new instance of the <see cref="FocusManager"/> class.
        /// </summary>
        public FocusManager()
        {
            InputElement.PointerPressedEvent.AddClassHandler(
                typeof(IInputElement),
                new EventHandler<RoutedEventArgs>(this.OnPreviewPointerPressed),
                RoutingStrategies.Tunnel);
        }

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
        /// <param name="method">The method by which focus was changed.</param>
        public void Focus(IInputElement control, NavigationMethod method = NavigationMethod.Unspecified)
        {
            if (control != null)
            {
                var scope = GetFocusScopeAncestors(control)
                    .FirstOrDefault();

                if (scope != null)
                {
                    this.Scope = scope;
                    this.SetFocusedElement(scope, control, method);
                }
            }
            else if (this.Current != null)
            {
                // If control is null, set focus to the topmost focus scope.
                foreach (var scope in GetFocusScopeAncestors(this.Current).Reverse().ToList())
                {
                    IInputElement element;

                    if (this.focusScopes.TryGetValue(scope, out element))
                    {
                        this.Focus(element, method);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Sets the currently focused element in the specified scope.
        /// </summary>
        /// <param name="scope">The focus scope.</param>
        /// <param name="element">The element to focus. May be null.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <remarks>
        /// If the specified scope is the current <see cref="Scope"/> then the keyboard focus
        /// will change.
        /// </remarks>
        public void SetFocusedElement(
            IFocusScope scope,
            IInputElement element,
            NavigationMethod method = NavigationMethod.Unspecified)
        {
            Contract.Requires<ArgumentNullException>(scope != null);

            this.focusScopes[scope] = element;

            if (this.Scope == scope)
            {
                KeyboardDevice.Instance.SetFocusedElement(element, method);
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

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        private static bool CanFocus(IInputElement e) => e.Focusable && e.IsEnabledCore && e.IsVisible;

        /// <summary>
        /// Gets the focus scope ancestors of the specified control, traversing popups.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The focus scopes.</returns>
        private static IEnumerable<IFocusScope> GetFocusScopeAncestors(IInputElement control)
        {
            while (control != null)
            {
                var scope = control as IFocusScope;

                if (scope != null)
                {
                    yield return scope;
                }

                control = control.GetVisualParent<IInputElement>() ??
                    ((control as IHostedVisualTreeRoot)?.Host as IInputElement);
            }
        }

        /// <summary>
        /// Global handler for pointer pressed events.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private void OnPreviewPointerPressed(object sender, RoutedEventArgs e)
        {
            if (sender == e.Source)
            {
                var ev = (PointerPressEventArgs)e;
                var element = (ev.Device.Captured as IInputElement) ?? (e.Source as IInputElement);

                if (element == null || !CanFocus(element))
                {
                    element = element.GetSelfAndVisualAncestors()
                        .OfType<IInputElement>()
                        .FirstOrDefault(x => CanFocus(x));
                }

                if (element != null)
                {
                    this.Focus(element, NavigationMethod.Pointer);
                }
            }
        }
    }
}
