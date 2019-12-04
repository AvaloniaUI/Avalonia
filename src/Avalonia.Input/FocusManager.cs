// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    public class FocusManager : IFocusManager
    {
        /// <summary>
        /// The focus scopes in which the focus is currently defined.
        /// </summary>
        private readonly Dictionary<IFocusScope, IInputElement> _focusScopes =
            new Dictionary<IFocusScope, IInputElement>();

        /// <summary>
        /// Initializes a new instance of the <see cref="FocusManager"/> class.
        /// </summary>
        static FocusManager()
        {
            InputElement.PointerPressedEvent.AddClassHandler(
                typeof(IInputElement),
                new EventHandler<RoutedEventArgs>(OnPreviewPointerPressed),
                RoutingStrategies.Tunnel);
        }

        /// <summary>
        /// Gets the instance of the <see cref="IFocusManager"/>.
        /// </summary>
        public static IFocusManager Instance => AvaloniaLocator.Current.GetService<IFocusManager>();

        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        public IInputElement Current => KeyboardDevice.Instance?.FocusedElement;

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
        /// <param name="modifiers">Any input modifiers active at the time of focus.</param>
        public void Focus(
            IInputElement control, 
            NavigationMethod method = NavigationMethod.Unspecified,
            InputModifiers modifiers = InputModifiers.None)
        {
            if (control != null)
            {
                var scope = GetFocusScopeAncestors(control)
                    .FirstOrDefault();

                if (scope != null)
                {
                    Scope = scope;
                    SetFocusedElement(scope, control, method, modifiers);
                }
            }
            else if (Current != null)
            {
                // If control is null, set focus to the topmost focus scope.
                foreach (var scope in GetFocusScopeAncestors(Current).Reverse().ToList())
                {
                    IInputElement element;

                    if (_focusScopes.TryGetValue(scope, out element) && element != null)
                    {
                        Focus(element, method);
                        return;
                    }
                }

                // Couldn't find a focus scope, clear focus.
                SetFocusedElement(Scope, null);
            }
        }

        /// <summary>
        /// Sets the currently focused element in the specified scope.
        /// </summary>
        /// <param name="scope">The focus scope.</param>
        /// <param name="element">The element to focus. May be null.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="modifiers">Any input modifiers active at the time of focus.</param>
        /// <remarks>
        /// If the specified scope is the current <see cref="Scope"/> then the keyboard focus
        /// will change.
        /// </remarks>
        public void SetFocusedElement(
            IFocusScope scope,
            IInputElement element,
            NavigationMethod method = NavigationMethod.Unspecified,
            InputModifiers modifiers = InputModifiers.None)
        {
            Contract.Requires<ArgumentNullException>(scope != null);

            _focusScopes[scope] = element;

            if (Scope == scope)
            {
                KeyboardDevice.Instance?.SetFocusedElement(element, method, modifiers);
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

            if (!_focusScopes.TryGetValue(scope, out e))
            {
                // TODO: Make this do something useful, i.e. select the first focusable
                // control, select a control that the user has specified to have default
                // focus etc.
                e = scope as IInputElement;
                _focusScopes.Add(scope, e);
            }

            Scope = scope;
            Focus(e);
        }

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        private static bool CanFocus(IInputElement e) => e.Focusable && e.IsEffectivelyEnabled && e.IsVisible;

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
        private static void OnPreviewPointerPressed(object sender, RoutedEventArgs e)
        {
            var ev = (PointerPressedEventArgs)e;

            if (sender == e.Source && ev.MouseButton == MouseButton.Left)
            {
                IVisual element = ev.Pointer?.Captured ?? e.Source as IInputElement;

                while (element != null)
                {
                    if (element is IInputElement inputElement && CanFocus(inputElement))
                    {
                        Instance?.Focus(inputElement, NavigationMethod.Pointer, ev.InputModifiers);

                        break;
                    }
                    
                    element = element.VisualParent;
                }
            }
        }
    }
}
