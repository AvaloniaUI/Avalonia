using System;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    public partial class FocusManager
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
            // Would this be better suited in TopLevel or InputElement rather than here?
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
            searchScope = ResolveSearchScope(searchScope);

            return TabNavigation.GetFirstTabInGroup(searchScope);            
        }

        /// <summary>
        /// Retrieves the last element that can receive focus based on the specified scope
        /// </summary>
        /// <param name="searchScope">The root object from which to search. If null, the search scope
        /// is the current TopLevel</param>
        /// <returns>The last focusable object</returns>
        public static IInputElement? FindLastFocusableElement(IInputElement? searchScope)
        {            
            searchScope = ResolveSearchScope(searchScope);

            return TabNavigation.GetLastTabInGroup(searchScope);
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
        public static IInputElement? FindNextElement(NavigationDirection focusNavigationDirection)
        {
            // TODO: This should be used to find elements that aren't InputElements that should be focusable
            // when/if support for that becomes available. WinUI references hyperlink objects which aren't
            // UIElements, but are still focusable
            // Return type should be changed to common base type when/if this happens (UWP uses DependencyObject)

            return FindNextElementInternal(focusNavigationDirection);
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
        public static IInputElement? FindNextElement(NavigationDirection focusNavigationDirection, FindNextElementOptions focusNavigationOptions)
        {
            // TODO: This should be used to find elements that aren't InputElements that should be focusable
            // when/if support for that becomes available. WinUI references hyperlink objects which aren't
            // UIElements, but are still focusable
            // Return type should be changed to common base type when/if this happens (UWP uses DependencyObject)

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
            return FindNextElement(focusNavigationDirection);
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
            var root = _activeFocusRoot ?? ResolveInitialFocusRoot();
            var currentFM = root.FocusManager as FocusManager;

            var result = FindNextElementInternal(focusNavigationDirection);

            if (result == null)
            {
                return false;
            }

            currentFM!.SetFocusedElement(result, focusNavigationDirection, FocusState.Programmatic);
            return true;
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
            throw new NotImplementedException("To be implemented with XYFocus");
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

            // Probably should use IsEffectivelyVisible here, but that walks the tree
            // every time and that could be an issue. Also how often would just 
            // IsVisible be insufficient?
            if (!element.IsVisible)
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

            //var root = element.VisualRoot;

            //// PopupRoot shouldn't be a TopLevel, but because it is, has a FocusManager instance with it
            //// inherited from TopLevel. But we want all windowed Popups to be treated in the same "tree" as the window
            //// its associated with, so if the visual root of the current element is a Popup,
            //// we need to find the TopLevel its attached to
            //if (root is PopupRoot pr)
            //{
            //    // Search up the logical tree to find the parent TopLevel that owns the Popup
            //    var tl = pr.FindLogicalAncestorOfType<TopLevel>();
            //    if (tl != null)
            //    {
            //        return tl.FocusManager;
            //    }

            //    // TODO: if this fail, probably should try to brute force it checking open windows on desktop or 
            //    // MainView on single app lifetime, find the OverlayLayer, and seeing if we can find the associated
            //    // windowed popup in its registry
            //}
            //else if (root is TopLevel tl)
            //{
            //    return tl.FocusManager;
            //}

            return null;
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

        internal static IInputRoot ResolveInitialFocusRoot()
        {
            // This should rarely be needed, but just in case TopLevel hasn't set focus or user has
            // cleared focus to 'null', we need to make sure we can find a root to move focus in
            //var app = Application.Current!.ApplicationLifetime;
            //IInputRoot? root = null;
            //if (app is IClassicDesktopStyleApplicationLifetime cdl)
            //{
            //    root = cdl.MainWindow;
            //}
            //else if (app is ISingleViewApplicationLifetime sal)
            //{
            //    root = sal.MainView?.FindAncestorOfType<IInputRoot>(true);
            //}

            //if (root == null)
            //{
            //    throw new InvalidOperationException("Unable to find a valid IInputRoot for initial focus");
            //}

            return null!;// root;
        }

        // TODO_FOCUS: In future, if non-IInputElement focusable items are supported, this return type should change
        private static IInputElement? FindNextElementInternal(NavigationDirection direction)
        {
            var currentFocus = GetFocusedElement();

            if (currentFocus == null)
            {
                var root = _activeFocusRoot ?? ResolveInitialFocusRoot();
                switch (direction)
                {
                    // If no focus exists yet, we'll use normal tab stop behavior regardless of direction
                    // to get focus directed into the app content. Grouping Right/Down with Next and
                    // Left/Up with Previous
                    case NavigationDirection.Next:
                    case NavigationDirection.Right:
                    case NavigationDirection.Down:
                        return TabNavigation.GetFirstTabInGroup(root);

                    case NavigationDirection.Previous:
                    case NavigationDirection.Left:
                    case NavigationDirection.Up:
                        return TabNavigation.GetLastTabInGroup(root);
                }

                throw new ArgumentException("Invalid NavigationDirection provided");
            }

            switch (direction)
            {
                case NavigationDirection.Next:
                    return TabNavigation.GetNextTab(currentFocus, false);

                case NavigationDirection.Previous:
                    return TabNavigation.GetPrevTab(currentFocus, null, false);

                default:
                    throw new NotImplementedException("To be implemented with XYFocus");
            }
        }

        private static IInputElement FindNextElementWithOptions(NavigationDirection direction, FindNextElementOptions options)
        {
            if (direction.IsTab())
            {
                throw new InvalidOperationException("NavigationDirection Next and Previous are not allowed" +
                   "when using FindNextElementOptions");
            }

            throw new NotImplementedException("To be implemented with XYFocus");
        }

        private static IInputElement ResolveSearchScope(IInputElement? scope)
        {
            // A null scope should use _activeFocusRoot or attempt to find the main view
            // if a focus action has not occurred yet
            if (scope == null)
            {
                if (_activeFocusRoot == null)
                {
                    _activeFocusRoot = ResolveInitialFocusRoot();

                    // If _activeFocusRoot is still null, user called this too early in app lifetime as no views
                    // exist yet
                    if (_activeFocusRoot == null)
                    {
                        throw new InvalidOperationException("Search scope is not specified and no " +
                            "active focus scope is currently available");
                    }
                }

                scope = _activeFocusRoot;
            }

            // If the search root is the TopLevel we handle this in a specific way
            // First, we check with the OverlayLayer
            // Popups and items in the OverlayLayer get first precedence. Only lightdismiss/popups
            // are eligible here *TODO (see GetTopMostLightDismissElement note)
            // If nothing in the OverlayLayer is available, we then move to the main app content
            if (scope is IInputRoot root)
            {
                var overlayLayer = root.OverlayHost;
                if (overlayLayer != null)
                {
                    var possSearchRoot = overlayLayer.GetTopmostLightDismissElement();

                    if (possSearchRoot != null)
                    {
                        return possSearchRoot;
                    }
                }
            }

            return scope;
        }
    }    
}
