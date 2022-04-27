using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Controls
{
    public partial class FocusManager : IFocusManager
    {
        private static IInputRoot? _activeFocusRoot;
        private static WeakReference<IInputRoot>? _previousFocusRoot;

        /// <summary>
        /// Occurs before an element actually receives focus
        /// </summary>
        public static event EventHandler<GettingFocusEventArgs>? GettingFocus;

        /// <summary>
        /// Occurs when an element receives focus. This method is raised asynchronously, so
        /// focus might move before bubbling is complete
        /// </summary>
        public static event EventHandler<FocusManagerGotFocusEventArgs>? GotFocus;

        /// <summary>
        /// Occurs before focus moves from the current element to the new target element.
        /// </summary>
        public static event EventHandler<LosingFocusEventArgs>? LosingFocus;

        /// <summary>
        /// Occurs when an element loses focus. This method is raised asynchronously, so
        /// focus might move before bubbling is complete
        /// </summary>
        public static event EventHandler<FocusManagerLostFocusEventArgs>? LostFocus;

        static FocusManager()
        {
            // Would this be better suited in TopLevel rather than here?
            InputElement.PointerPressedEvent.AddClassHandler(
                typeof(IInputElement),
                new EventHandler<RoutedEventArgs>(OnPreviewPointerPressed),
                RoutingStrategies.Tunnel);
        }

        private static void OnPreviewPointerPressed(object? sender, RoutedEventArgs e)
        {
            if (sender is null)
                return;

            var ev = (PointerPressedEventArgs)e;
            var visual = (IVisual)sender;

            if (sender == e.Source && ev.GetCurrentPoint(visual).Properties.IsLeftButtonPressed)
            {
                IVisual? element = ev.Pointer?.Captured ?? e.Source as IInputElement;

                var fm = GetFocusManagerFromElement(element as IInputElement);
                if (fm == null)
                {
                    return;
                }

                while (element != null)
                {
                    if (element is IInputElement inputElement && IsFocusable(inputElement))
                    {
                        fm.SetFocusedElement(inputElement, state: FocusState.Pointer);

                        break;
                    }

                    element = element.VisualParent;
                }
            }
        }

        /// <summary>
        /// Retreieves the element in the active UI that has focus
        /// </summary>
        /// <returns></returns>
        public static IInputElement? GetFocusedElement()
        {
            return (_activeFocusRoot?.FocusManager as FocusManager)?.FocusedElement;
        }

        /// <summary>
        /// Retreives the first element that can receive focus based on the specified scope
        /// </summary>
        /// <param name="searchScope">The root object from which to search. If null, the search scope
        /// is the current TopLevel</param>
        /// <returns>The first focusable object</returns>
        public static IInputElement? FindFirstFocusableElement(IInputElement? searchScope)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the last element that can receive focus based on the specified scope
        /// </summary>
        /// <param name="searchScope">The root object from which to search. If null, the search scope
        /// is the current TopLevel</param>
        /// <returns>The last focusable object</returns>
        public static IInputElement? FindLastFocusableElement(IInputElement? searchScope)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the element that should receive focus based on the specified navigation direction.
        /// </summary>
        /// <param name="focusNavigationDirection">The direction the focus should move</param>
        /// <returns>The next object to receive focus</returns>
        /// <remarks>
        /// We recommend you use this method instead of <see cref="FindNextFocusableElement(NavigationDirection)"/> as
        /// FindNextFocusableElement will return null if the next focusable element is not an <see cref="IInputElement"/> 
        /// (such as a hyperlink object)
        /// </remarks>
        public static AvaloniaObject? FindNextElement(NavigationDirection focusNavigationDirection)
        {
            // TODO: This should be used to find elements that aren't InputElements that should be focusable
            // when/if support for that becomes available. WinUI references hyperlink objects which aren't
            // UIElements, but are still focusable

            throw new NotImplementedException();
        }

        /// <summary>
        /// Retreieves the element that should receive focus based on the specified navigation direction (cannot be used
        /// with tab navigation)
        /// </summary>
        /// <param name="focusNavigationDirection">The direction focus should move</param>
        /// <param name="focusNavigationOptions">The options to help identify the next element to receive focus</param>
        /// <returns>The next element to receive focus</returns>
        /// <remarks>
        /// Only directional navigation may be used in this method, <see cref="NavigationDirection.Previous"/> and 
        /// <see cref="NavigationDirection.Next"/> are invalid and will throw
        /// </remarks>
        public static AvaloniaObject? FindNextElement(NavigationDirection focusNavigationDirection, FindNextElementOptions focusNavigationOptions)
        {
            throw new NotImplementedException("To be implemented with XYFocus");
        }

        /// <summary>
        /// Retreives the next element that should receive focus based on the specified navigation direction
        /// </summary>
        /// <param name="focusNavigationDirection">The direction that focus should move</param>
        /// <returns>The next focusable IInputElement, or null if focus cannot be set in the specified direction</returns>
        public static IInputElement? FindNextFocusableElement(NavigationDirection focusNavigationDirection)
        {
            // For now this will just call FindNextElement
            return FindNextElement(focusNavigationDirection) as IInputElement;
        }

        /// <summary>
        /// Retreives the next element that should receive focus based on the specified navigation direction
        /// </summary>
        /// <param name="focusNavigationDirection">The direction that focus should move</param>
        /// <param name="hintRect">A bounding rect used to influence which elements are most likely
        /// to be considered for next focus</param>
        /// <returns>The next focusable IInputElement, or null if focus cannot be set in the specified direction</returns>
        public static IInputElement? FindNextFocusableElement(NavigationDirection focusNavigationDirection, Rect hintRect)
        {
            throw new NotImplementedException("To be implemented with XYFocus");
        }

        /// <summary>
        /// Attempts to change focus from the element with focus to the next focusable element in the specified direction
        /// </summary>
        /// <param name="focusNavigationDirection">The direction to traverse (in tab order)</param>
        /// <returns>true if focus moved; otherwise, false</returns>
        public static bool TryMoveFocus(NavigationDirection focusNavigationDirection)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Attempts to change focus from the element with focus to the next focusable element in the specified direction
        /// using the specified navigation options
        /// </summary>
        /// <param name="focusNavigationDirection">The direction to traverse (in tab order)</param>
        /// <param name="focusNavigationOptions">The options to help identify the next element to receive focus</param>
        /// <returns>true if focus moved; otherwise, false</returns>
        public static bool TryMoveFocus(NavigationDirection focusNavigationDirection, FindNextElementOptions focusNavigationOptions)
        {
            throw new NotImplementedException();
        }


        // Not implemented as docs say this is for XamlIslands
        // public static IInputElement? GetFocusedElement(TopLevel focusRoot);

        // The following async methods are not implemented 
        // public static IInputElement? TryFocusAsync(IInputElement? object, FocusState state);
        // public static bool TryMoveFocusAsync(NavigationDirection direction);
        // public static bool TryMoveFocusAsync(FocusNavigationDirection direction, FindNextElementOptions options);

        internal static bool IsFocusable(IInputElement? element)
        {
            if (element == null || !element.Focusable)
                return false;

            if (element is InputElement ie && !KeyboardNavigation.GetIsTabStop(ie))
                return false;

            // This is cached
            if (!element.IsEffectivelyEnabled && !element.AllowFocusWhenDisabled)
                return false;

            // This walks the whole tree up
            if (!element.IsEffectivelyVisible)
                return false;

            return true;
        }

        internal static FocusManager? GetFocusManagerFromElement(IInputElement? element)
        {
            if (element == null)
                return null;

            var fm = ((element.VisualRoot as IInputRoot)?.FocusManager as FocusManager) ??
                element.FindAncestorOfType<IInputRoot>()?.FocusManager as FocusManager;

            if (fm == null)
            {
                throw new InvalidOperationException("Unable to find IInputRoot or FocusManager for given element");
            }

            return fm;
        }

        internal static FocusState GetActualFocusState(FocusInputDeviceKind lastInputType, FocusState state)
        {
            if (state != FocusState.Programmatic)
                return state;

            switch (lastInputType)
            {
                case FocusInputDeviceKind.Keyboard:
                case FocusInputDeviceKind.GameController: // Not implemented, but we'll include it anyway
                    return FocusState.Keyboard;

                default:
                    return FocusState.Pointer;
            }
        }
    }

    // TODO: Separate this out
    public class FindNextElementOptions
    {
        public XYFocusNavigationStrategy XYFocusNavigationStrategyOverride { get; set; }

        public IInputElement? SearchRoot { get; set; }

        public Rect HintRect { get; set; }

        public Rect ExclusionRect { get; set; }
    }
}
