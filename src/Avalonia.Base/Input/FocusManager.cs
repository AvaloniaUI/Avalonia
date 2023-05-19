using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
    [PrivateApi]
    public class FocusManager : IFocusManager
    {
        /// <summary>
        /// The focus scopes in which the focus is currently defined.
        /// </summary>
        private readonly ConditionalWeakTable<IFocusScope, IInputElement?> _focusScopes =
            new ConditionalWeakTable<IFocusScope, IInputElement?>();

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

        private IInputElement? Current => KeyboardDevice.Instance?.FocusedElement;

        /// <summary>
        /// Gets the currently focused <see cref="IInputElement"/>.
        /// </summary>
        public IInputElement? GetFocusedElement() => Current;

        /// <summary>
        /// Gets the current focus scope.
        /// </summary>
        public IFocusScope? Scope
        {
            get;
            private set;
        }

        /// <summary>
        /// Focuses a control.
        /// </summary>
        /// <param name="control">The control to focus.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        public bool Focus(
            IInputElement? control, 
            NavigationMethod method = NavigationMethod.Unspecified,
            KeyModifiers keyModifiers = KeyModifiers.None)
        {
            if (control != null)
            {
                var scope = GetFocusScopeAncestors(control)
                    .FirstOrDefault();

                if (scope != null)
                {
                    Scope = scope;
                    return SetFocusedElement(scope, control, method, keyModifiers);
                }
            }
            else if (Current != null)
            {
                // If control is null, set focus to the topmost focus scope.
                foreach (var scope in GetFocusScopeAncestors(Current).Reverse().ToArray())
                {
                    if (scope != Scope &&
                        _focusScopes.TryGetValue(scope, out var element) &&
                        element != null)
                    {
                        return Focus(element, method);
                    }
                }

                if (Scope is object)
                {
                    // Couldn't find a focus scope, clear focus.
                    return SetFocusedElement(Scope, null);
                }
            }

            return false;
        }

        public void ClearFocus()
        {
            Focus(null);
        }

        public IInputElement? GetFocusedElement(IFocusScope scope)
        {
            _focusScopes.TryGetValue(scope, out var result);
            return result;
        }

        /// <summary>
        /// Sets the currently focused element in the specified scope.
        /// </summary>
        /// <param name="scope">The focus scope.</param>
        /// <param name="element">The element to focus. May be null.</param>
        /// <param name="method">The method by which focus was changed.</param>
        /// <param name="keyModifiers">Any key modifiers active at the time of focus.</param>
        /// <remarks>
        /// If the specified scope is the current <see cref="Scope"/> then the keyboard focus
        /// will change.
        /// </remarks>
        public bool SetFocusedElement(
            IFocusScope scope,
            IInputElement? element,
            NavigationMethod method = NavigationMethod.Unspecified,
            KeyModifiers keyModifiers = KeyModifiers.None)
        {
            scope = scope ?? throw new ArgumentNullException(nameof(scope));

            if (element is not null && !CanFocus(element))
            {
                return false;
            }

            if (_focusScopes.TryGetValue(scope, out var existingElement))
            {
                if (element != existingElement)
                {
                    _focusScopes.Remove(scope);
                    _focusScopes.Add(scope, element);
                }
            }
            else
            {
                _focusScopes.Add(scope, element);
            }

            if (Scope == scope)
            {
                KeyboardDevice.Instance?.SetFocusedElement(element, method, keyModifiers);
            }

            return true;
        }

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        public void SetFocusScope(IFocusScope scope)
        {
            scope = scope ?? throw new ArgumentNullException(nameof(scope));

            if (!_focusScopes.TryGetValue(scope, out var e))
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

        public void RemoveFocusScope(IFocusScope scope)
        {
            scope = scope ?? throw new ArgumentNullException(nameof(scope));
            
            if (_focusScopes.TryGetValue(scope, out _))
            {
                SetFocusedElement(scope, null);
                _focusScopes.Remove(scope);
            }

            if (Scope == scope)
            {
                Scope = null;
            }
        }

        public static bool GetIsFocusScope(IInputElement e) => e is IFocusScope;

        /// <summary>
        /// Public API customers should use TopLevel.GetTopLevel(control).FocusManager.
        /// But since we have split projects, we can't access TopLevel from Avalonia.Base.
        /// That's why we need this helper method instead.
        /// </summary>
        internal static FocusManager? GetFocusManager(IInputElement? element)
        {
            // Element might not be a visual, and not attached to the root.
            // But IFocusManager is always expected to be a FocusManager. 
            return (FocusManager?)((element as Visual)?.VisualRoot as IInputRoot)?.FocusManager
                   // In our unit tests some elements might not have a root. Remove when we migrate to headless tests.
                ?? (FocusManager?)AvaloniaLocator.Current.GetService<IFocusManager>();
        }
        
        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        private static bool CanFocus(IInputElement e) => e.Focusable && e.IsEffectivelyEnabled && IsVisible(e);

        /// <summary>
        /// Gets the focus scope ancestors of the specified control, traversing popups.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The focus scopes.</returns>
        private static IEnumerable<IFocusScope> GetFocusScopeAncestors(IInputElement control)
        {
            IInputElement? c = control;

            while (c != null)
            {
                if (c is IFocusScope scope &&
                    c is Visual v &&
                    v.VisualRoot is Visual root &&
                    root.IsVisible)
                {
                    yield return scope;
                }

                c = (c as Visual)?.GetVisualParent<IInputElement>() ??
                    ((c as IHostedVisualTreeRoot)?.Host as IInputElement);
            }
        }

        /// <summary>
        /// Global handler for pointer pressed events.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        private static void OnPreviewPointerPressed(object? sender, RoutedEventArgs e)
        {
            if (sender is null)
                return;

            var ev = (PointerPressedEventArgs)e;
            var visual = (Visual)sender;

            if (sender == e.Source && ev.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
            {
                Visual? element = ev.Pointer?.Captured as Visual ?? e.Source as Visual;

                while (element != null)
                {
                    if (element is IInputElement inputElement && CanFocus(inputElement))
                    {
                        inputElement.Focus(NavigationMethod.Pointer, ev.KeyModifiers);

                        break;
                    }
                    
                    element = element.VisualParent;
                }
            }
        }

        private static bool IsVisible(IInputElement e) => (e as Visual)?.IsEffectivelyVisible ?? true;
    }
}
