using System;
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
        /// Private attached property for storing the currently focused element in a focus scope.
        /// </summary>
        /// <remarks>
        /// This property is set on the control which defines a focus scope and tracks the currently
        /// focused element within that scope.
        /// </remarks>
        private static readonly AttachedProperty<IInputElement> FocusedElementProperty =
            AvaloniaProperty.RegisterAttached<FocusManager, StyledElement, IInputElement>("FocusedElement");

        private StyledElement? _focusRoot;

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
            if (KeyboardDevice.Instance is not { } keyboardDevice)
                return false;

            if (control is not null)
            {
                if (!CanFocus(control))
                    return false;

                if (GetFocusScope(control) is StyledElement scope)
                {
                    scope.SetValue(FocusedElementProperty, control);
                    _focusRoot = GetFocusRoot(scope);
                }

                keyboardDevice.SetFocusedElement(control, method, keyModifiers);
                return true;
            }
            else if (_focusRoot?.GetValue(FocusedElementProperty) is { } restore && 
                restore != Current &&
                Focus(restore))
            {
                return true;
            }
            else
            {
                _focusRoot = null;
                keyboardDevice.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None);
                return false;
            }
        }

        public void ClearFocus()
        {
            Focus(null);
        }

        public void ClearFocusOnElementRemoved(IInputElement removedElement, Visual oldParent)
        {
            if (oldParent is IInputElement parentElement &&
                GetFocusScope(parentElement) is StyledElement scope &&
                scope.GetValue(FocusedElementProperty) is IInputElement focused &&
                focused == removedElement)
            {
                scope.ClearValue(FocusedElementProperty);
            }

            if (Current == removedElement)
                Focus(null);
        }

        public IInputElement? GetFocusedElement(IFocusScope scope)
        {
            return (scope as StyledElement)?.GetValue(FocusedElementProperty);
        }

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        public void SetFocusScope(IFocusScope scope)
        {
            if (GetFocusedElement(scope) is { } focused)
            {
                Focus(focused);
            }
            else if (scope is IInputElement scopeElement && CanFocus(scopeElement))
            {
                // TODO: Make this do something useful, i.e. select the first focusable
                // control, select a control that the user has specified to have default
                // focus etc.
                Focus(scopeElement);
            }
        }

        public void RemoveFocusRoot(IFocusScope scope)
        {
            if (scope == _focusRoot)
                ClearFocus();
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

        internal bool TryMoveFocus(NavigationDirection direction)
        {
            if (GetFocusedElement() is {} focusedElement
                && KeyboardNavigationHandler.GetNext(focusedElement, direction) is {} newElement)
            {
                return newElement.Focus();
            }

            return false;
        }
        
        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        internal static bool CanFocus(IInputElement e) => e.Focusable && e.IsEffectivelyEnabled && IsVisible(e);

        /// <summary>
        /// Gets the focus scope of the specified control, traversing popups.
        /// </summary>
        /// <param name="control">The control.</param>
        /// <returns>The focus scope.</returns>
        private static StyledElement? GetFocusScope(IInputElement control)
        {
            IInputElement? c = control;

            while (c != null)
            {
                if (c is IFocusScope &&
                    c is Visual v &&
                    v.VisualRoot is Visual root &&
                    root.IsVisible)
                {
                    return v;
                }

                c = (c as Visual)?.GetVisualParent<IInputElement>() ??
                    ((c as IHostedVisualTreeRoot)?.Host as IInputElement);
            }

            return null;
        }

        private static StyledElement? GetFocusRoot(StyledElement scope)
        {
            if (scope is not Visual v)
                return null;

            var root = v.VisualRoot as Visual;

            while (root is IHostedVisualTreeRoot hosted &&
                hosted.Host?.VisualRoot is Visual parentRoot)
            {
                root = parentRoot;
            }

            return root;
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

        private static bool IsVisible(IInputElement e)
        {
            if (e is Visual v)
                return v.IsAttachedToVisualTree && e.IsEffectivelyVisible;
            return true;
        }
    }
}
