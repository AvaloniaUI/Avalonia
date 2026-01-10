using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Input.Navigation;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Reactive;
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
                new EventHandler<RoutedEventArgs>(OnPreviewPointerEventHandler),
                RoutingStrategies.Tunnel);
            InputElement.PointerReleasedEvent.AddClassHandler(
                typeof(IInputElement),
                new EventHandler<RoutedEventArgs>(OnPreviewPointerEventHandler),
                RoutingStrategies.Tunnel);
        }

        public FocusManager()
        {
            _contentRoot = null;
        }

        public FocusManager(IInputElement contentRoot)
        {
            _contentRoot = contentRoot;
        }

        private IInputElement? Current => KeyboardDevice.Instance?.FocusedElement;

        private XYFocus _xyFocus = new();
        private XYFocusOptions _xYFocusOptions = new XYFocusOptions();
        private IInputElement? _contentRoot;

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
                keyboardDevice.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, false);
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

        /// <summary>
        /// Attempts to change focus from the element with focus to the next focusable element in the specified direction.
        /// </summary>
        /// <param name="direction">The direction to traverse (in tab order).</param>
        /// <returns>true if focus moved; otherwise, false.</returns>
        public bool TryMoveFocus(NavigationDirection direction)
        {
            return FindAndSetNextFocus(direction, _xYFocusOptions);
        }

        /// <summary>
        /// Attempts to change focus from the element with focus to the next focusable element in the specified direction, using the specified navigation options.
        /// </summary>
        /// <param name="direction">The direction to traverse (in tab order).</param>
        /// <param name="options">The options to help identify the next element to receive focus with keyboard/controller/remote navigation.</param>
        /// <returns>true if focus moved; otherwise, false.</returns>
        public bool TryMoveFocus(NavigationDirection direction, FindNextElementOptions options)
        {
            return FindAndSetNextFocus(direction, ValidateAndCreateFocusOptions(direction, options));
        }

        /// <summary>
        /// Checks if the specified element can be focused.
        /// </summary>
        /// <param name="e">The element.</param>
        /// <returns>True if the element can be focused.</returns>
        internal static bool CanFocus(IInputElement e) => e.Focusable && e.IsEffectivelyEnabled && IsVisible(e);

        private static bool CanPointerFocus(IInputElement e, PointerEventArgs ev)
        {
            if (CanFocus(e))
            {
                if (ev.Pointer.Type == PointerType.Mouse || ev is PointerReleasedEventArgs)
                    return true;
            }

            return false;
        }

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
        private static void OnPreviewPointerEventHandler(object? sender, RoutedEventArgs e)
        {
            if (sender is null)
                return;

            var ev = (PointerEventArgs)e;
            var visual = (Visual)sender;

            if (sender == e.Source && (ev.GetCurrentPoint(visual).Properties.IsLeftButtonPressed || (e as PointerReleasedEventArgs)?.InitialPressMouseButton == MouseButton.Left))
            {
                Visual? element = ev.Pointer?.Captured as Visual ?? e.Source as Visual;

                while (element != null)
                {
                    if (element is IInputElement inputElement && CanPointerFocus(inputElement, ev))
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

        /// <summary>
        /// Retrieves the first element that can receive focus.
        /// </summary>
        /// <returns>The first focusable element.</returns>
        public IInputElement? FindFirstFocusableElement()
        {
            var root = (_contentRoot as Visual)?.GetSelfAndVisualDescendants().FirstOrDefault(x => x is IInputElement) as IInputElement;
            if (root == null)
                return null;
            return GetFirstFocusableElementFromRoot(false);
        }

        /// <summary>
        /// Retrieves the first element that can receive focus based on the specified scope.
        /// </summary>
        /// <param name="searchScope">The root element from which to search.</param>
        /// <returns>The first focusable element.</returns>
        public static IInputElement? FindFirstFocusableElement(IInputElement searchScope)
        {
            return GetFirstFocusableElement(searchScope);
        }

        /// <summary>
        /// Retrieves the last element that can receive focus.
        /// </summary>
        /// <returns>The last focusable element.</returns>
        public IInputElement? FindLastFocusableElement()
        {
            var root = (_contentRoot as Visual)?.GetSelfAndVisualDescendants().FirstOrDefault(x => x is IInputElement) as IInputElement;
            if (root == null)
                return null;
            return GetFirstFocusableElementFromRoot(true);
        }

        /// <summary>
        /// Retrieves the last element that can receive focus based on the specified scope.
        /// </summary>
        /// <param name="searchScope">The root element from which to search.</param>
        /// <returns>The last focusable object.</returns>
        public static IInputElement? FindLastFocusableElement(IInputElement searchScope)
        {
            return GetFocusManager(searchScope)?.GetLastFocusableElement(searchScope);
        }

        /// <summary>
        /// Retrieves the element that should receive focus based on the specified navigation direction.
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public IInputElement? FindNextElement(NavigationDirection direction)
        {
            var xyOption = new XYFocusOptions()
            {
                UpdateManifold = false
            };

            return FindNextFocus(direction, xyOption);
        }

        /// <summary>
        /// Retrieves the element that should receive focus based on the specified navigation direction (cannot be used with tab navigation).
        /// </summary>
        /// <param name="direction">The direction that focus moves from element to element within the app UI.</param>
        /// <param name="options">The options to help identify the next element to receive focus with the provided navigation.</param>
        /// <returns>The next element to receive focus.</returns>
        public IInputElement? FindNextElement(NavigationDirection direction, FindNextElementOptions options)
        {
            return FindNextFocus(direction, ValidateAndCreateFocusOptions(direction, options));
        }

        private static XYFocusOptions ValidateAndCreateFocusOptions(NavigationDirection direction, FindNextElementOptions options)
        {
            if (direction is not NavigationDirection.Up
                and not NavigationDirection.Down
                and not NavigationDirection.Left
                and not NavigationDirection.Right)
            {
                throw new ArgumentOutOfRangeException(nameof(direction),
                        $"{direction} is not supported with FindNextElementOptions. Only Up, Down, Left and right are supported");
            }

            return new XYFocusOptions
            {
                UpdateManifold = false,
                SearchRoot = options.SearchRoot,
                ExclusionRect = options.ExclusionRect,
                FocusHintRectangle = options.FocusHintRectangle,
                NavigationStrategyOverride = options.NavigationStrategyOverride,
                IgnoreOcclusivity = options.IgnoreOcclusivity
            };
        }

        internal IInputElement? FindNextFocus(NavigationDirection direction, XYFocusOptions focusOptions, bool updateManifolds = true)
        {
            IInputElement? nextFocusedElement = null;

            var currentlyFocusedElement = Current;

            if (direction is NavigationDirection.Previous or NavigationDirection.Next || currentlyFocusedElement == null)
            {
                var isReverse = direction == NavigationDirection.Previous;
                nextFocusedElement = ProcessTabStopInternal(isReverse, true);
            }
            else
            {
                if (currentlyFocusedElement is InputElement inputElement &&
                    XYFocus.GetBoundsForRanking(inputElement, focusOptions.IgnoreClipping) is { } bounds)
                {
                    focusOptions.FocusedElementBounds = bounds;
                }

                nextFocusedElement = _xyFocus.GetNextFocusableElement(direction,
                    currentlyFocusedElement as InputElement,
                    null,
                    updateManifolds,
                    focusOptions);
            }

            return nextFocusedElement;
        }

        internal static IInputElement? GetFirstFocusableElementInternal(IInputElement searchStart, IInputElement? focusCandidate = null)
        {
            IInputElement? firstFocusableFromCallback = null;
            var useFirstFocusableFromCallback = false;
            if (searchStart is InputElement inputElement)
            {
                firstFocusableFromCallback = inputElement.GetFirstFocusableElementOverride();

                if (firstFocusableFromCallback != null)
                {
                    useFirstFocusableFromCallback = FocusHelpers.IsFocusable(firstFocusableFromCallback) || FocusHelpers.CanHaveFocusableChildren(firstFocusableFromCallback as AvaloniaObject);
                }
            }

            if (useFirstFocusableFromCallback)
            {
                if (focusCandidate == null || (GetTabIndex(firstFocusableFromCallback) < GetTabIndex(focusCandidate)))
                {
                    focusCandidate = firstFocusableFromCallback;
                }
            }
            else
            {
                var children = FocusHelpers.GetInputElementChildren(searchStart as AvaloniaObject);

                foreach (var child in children)
                {
                    if (FocusHelpers.IsVisible(child))
                    {
                        var hasFocusableChildren = FocusHelpers.CanHaveFocusableChildren(child as AvaloniaObject);
                        if (FocusHelpers.IsPotentialTabStop(child))
                        {
                            if (focusCandidate == null && (FocusHelpers.IsFocusable(child) || hasFocusableChildren))
                            {
                                focusCandidate = child;
                            }

                            if (FocusHelpers.IsFocusable(child) || hasFocusableChildren)
                            {
                                if (focusCandidate == null || GetTabIndex(child) < GetTabIndex(focusCandidate))
                                {
                                    focusCandidate = child;
                                }
                            }
                        }
                        else if (hasFocusableChildren)
                        {
                            focusCandidate = GetFirstFocusableElementInternal(child, focusCandidate);
                        }
                    }
                }
            }

            return focusCandidate;
        }

        internal static IInputElement? GetLastFocusableElementInternal(IInputElement searchStart, IInputElement? lastFocus = null)
        {
            IInputElement? lastFocusableFromCallback = null;
            var useLastFocusableFromCallback = false;
            if (searchStart is InputElement inputElement)
            {
                lastFocusableFromCallback = inputElement.GetLastFocusableElementOverride();

                if (lastFocusableFromCallback != null)
                {
                    useLastFocusableFromCallback = FocusHelpers.IsFocusable(lastFocusableFromCallback) || FocusHelpers.CanHaveFocusableChildren(lastFocusableFromCallback as AvaloniaObject);
                }
            }

            if (useLastFocusableFromCallback)
            {
                if (lastFocus == null || (GetTabIndex(lastFocusableFromCallback) > GetTabIndex(lastFocus)))
                {
                    lastFocus = lastFocusableFromCallback;
                }
            }
            else
            {
                var children = FocusHelpers.GetInputElementChildren(searchStart as AvaloniaObject);

                foreach (var child in children)
                {
                    if (FocusHelpers.IsVisible(child))
                    {
                        var hasFocusableChildren = FocusHelpers.CanHaveFocusableChildren(child as AvaloniaObject);
                        if (FocusHelpers.IsPotentialTabStop(child))
                        {
                            if (lastFocus == null && (FocusHelpers.IsFocusable(child) || hasFocusableChildren))
                            {
                                lastFocus = child;
                            }

                            if (FocusHelpers.IsFocusable(child) || hasFocusableChildren)
                            {
                                if (lastFocus == null || GetTabIndex(child) >= GetTabIndex(lastFocus))
                                {
                                    lastFocus = child;
                                }
                            }
                        }
                        else if (hasFocusableChildren)
                        {
                            lastFocus = GetLastFocusableElementInternal(child, lastFocus);
                        }
                    }
                }
            }

            return lastFocus;
        }

        private IInputElement? ProcessTabStopInternal(bool isReverse, bool queryOnly)
        {
            IInputElement? newTabStop = null;

            var defaultCandidateTabStop = GetTabStopCandidateElement(isReverse, queryOnly, out var didCycleFocusAtRootVisualScope);

            var isTabStopOverriden = InputElement.ProcessTabStop(_contentRoot,
                Current,
                defaultCandidateTabStop,
                isReverse,
                didCycleFocusAtRootVisualScope,
                out var newTabStopFromCallback);

            if (isTabStopOverriden)
            {
                newTabStop = newTabStopFromCallback;
            }

            if (!isTabStopOverriden && newTabStop == null && defaultCandidateTabStop != null)
            {
                newTabStop = defaultCandidateTabStop;
            }

            return newTabStop;
        }

        private IInputElement? GetTabStopCandidateElement(bool isReverse, bool queryOnly, out bool didCycleFocusAtRootVisualScope)
        {
            didCycleFocusAtRootVisualScope = false;
            var currentFocus = Current;
            IInputElement? newTabStop = null;
            var root = this._contentRoot as IInputElement;

            if (root == null)
                return null;

            bool internalCycleWorkaround = false;

            if (Current != null)
            {
                internalCycleWorkaround = CanProcessTabStop(isReverse);
            }

            if (currentFocus == null)
            {
                if (!isReverse)
                {
                    newTabStop = GetFirstFocusableElement(root, null);
                }
                else
                {
                    newTabStop = GetLastFocusableElement(root, null);
                }

                didCycleFocusAtRootVisualScope = true;
            }
            else if (!isReverse)
            {
                newTabStop = GetNextTabStop();

                if (newTabStop == null && (internalCycleWorkaround || queryOnly))
                {
                    newTabStop = GetFirstFocusableElement(root, null);

                    didCycleFocusAtRootVisualScope = true;
                }
            }
            else
            {
                newTabStop = GetPreviousTabStop();

                if (newTabStop == null && (internalCycleWorkaround || queryOnly))
                {
                    newTabStop = GetLastFocusableElement(root, null);
                    didCycleFocusAtRootVisualScope = true;
                }
            }

            return newTabStop;
        }

        private IInputElement? GetNextTabStop(IInputElement? currentTabStop = null, bool ignoreCurrentTabStop = false)
        {
            var focused = currentTabStop ?? Current;
            if (focused == null || _contentRoot == null)
            {
                return null;
            }

            IInputElement? currentCompare = focused;
            IInputElement? newTabStop = (focused as InputElement)?.GetNextTabStopOverride();

            if (newTabStop == null && !ignoreCurrentTabStop
                && (FocusHelpers.IsVisible(focused) && (FocusHelpers.CanHaveFocusableChildren(focused as AvaloniaObject) || FocusHelpers.CanHaveChildren(focused))))
            {
                newTabStop = GetFirstFocusableElement(focused, newTabStop);
            }

            if (newTabStop == null)
            {
                var currentPassed = false;
                var current = focused;
                var parent = FocusHelpers.GetFocusParent(focused);
                var parentIsRootVisual = parent == (_contentRoot as Visual)?.VisualRoot;

                while (parent != null && !parentIsRootVisual && newTabStop == null)
                {
                    if (IsValidTabStopSearchCandidate(current) && current is InputElement c && KeyboardNavigation.GetTabNavigation(c) == KeyboardNavigationMode.Cycle)
                    {
                        if (current == GetParentTabStopElement(focused))
                        {
                            newTabStop = GetFirstFocusableElement(focused, null);
                        }
                        else
                        {
                            newTabStop = GetFirstFocusableElement(current, current);
                        }
                        break;
                    }

                    if (IsValidTabStopSearchCandidate(parent) && parent is InputElement p && KeyboardNavigation.GetTabNavigation(p) == KeyboardNavigationMode.Once)
                    {
                        current = parent;
                        parent = FocusHelpers.GetFocusParent(focused);
                        if (parent == null)
                            break;
                    }
                    else if (!IsValidTabStopSearchCandidate(parent))
                    {
                        var parentElement = GetParentTabStopElement(parent);
                        if (parentElement == null)
                        {
                            parent = GetRootOfPopupSubTree(current) as IInputElement;

                            if (parent != null)
                            {
                                newTabStop = GetNextOrPreviousTabStopInternal(parent, current, newTabStop, true, ref currentPassed, ref currentCompare);

                                if (newTabStop != null && !FocusHelpers.IsFocusable(newTabStop))
                                {
                                    newTabStop = GetFirstFocusableElement(newTabStop, null);
                                }
                                if (newTabStop == null)
                                {
                                    newTabStop = GetFirstFocusableElement(parent, null);
                                }
                                break;
                            }

                            parent = (_contentRoot as Visual)?.VisualRoot as IInputElement;
                        }
                        else if (parentElement is InputElement pIE && KeyboardNavigation.GetTabNavigation(pIE) == KeyboardNavigationMode.None)
                        {
                            current = pIE;
                            parent = FocusHelpers.GetFocusParent(current);
                            if (parent == null)
                                break;
                        }
                        else
                        {
                            parent = parentElement as IInputElement;
                        }
                    }

                    newTabStop = GetNextOrPreviousTabStopInternal(parent, current, newTabStop, true, ref currentPassed, ref currentCompare);

                    if (newTabStop != null && !FocusHelpers.IsFocusable(newTabStop) && FocusHelpers.CanHaveFocusableChildren(newTabStop as AvaloniaObject))
                    {
                        newTabStop = GetFirstFocusableElement(newTabStop, null);
                    }

                    if (newTabStop != null)
                        break;

                    if (IsValidTabStopSearchCandidate(parent))
                    {
                        current = parent;
                    }

                    parent = FocusHelpers.GetFocusParent(parent);
                    currentPassed = false;

                    parentIsRootVisual = parent == (_contentRoot as Visual)?.VisualRoot;
                }
            }

            return newTabStop;
        }

        private IInputElement? GetPreviousTabStop(IInputElement? currentTabStop = null, bool ignoreCurrentTabStop = false)
        {
            var focused = currentTabStop ?? Current;
            if (focused == null || _contentRoot == null)
            {
                return null;
            }
            IInputElement? newTabStop = (focused as InputElement)?.GetPreviousTabStopOverride();
            IInputElement? currentCompare = focused;

            if (newTabStop == null)
            {
                var currentPassed = false;
                var current = focused;
                var parent = FocusHelpers.GetFocusParent(focused);
                var parentIsRootVisual = parent == (_contentRoot as Visual)?.VisualRoot;

                while (parent != null && !parentIsRootVisual && newTabStop == null)
                {
                    if (IsValidTabStopSearchCandidate(current) && current is InputElement c && KeyboardNavigation.GetTabNavigation(c) == KeyboardNavigationMode.Cycle)
                    {
                        newTabStop = GetFirstFocusableElement(current, current);
                        break;
                    }

                    if (IsValidTabStopSearchCandidate(parent) && parent is InputElement p && KeyboardNavigation.GetTabNavigation(p) == KeyboardNavigationMode.Once)
                    {
                        if (FocusHelpers.IsFocusable(parent))
                        {
                            newTabStop = parent;
                        }
                        else
                        {
                            current = parent;
                            parent = FocusHelpers.GetFocusParent(focused);
                            if (parent == null)
                                break;
                        }
                    }
                    else if (!IsValidTabStopSearchCandidate(parent))
                    {
                        var parentElement = GetParentTabStopElement(parent);
                        if (parentElement == null)
                        {
                            parent = GetRootOfPopupSubTree(current) as IInputElement;

                            if (parent != null)
                            {
                                newTabStop = GetNextOrPreviousTabStopInternal(parent, current, newTabStop, false, ref currentPassed, ref currentCompare);

                                if (newTabStop != null && !FocusHelpers.IsFocusable(newTabStop))
                                {
                                    newTabStop = GetLastFocusableElement(newTabStop, null);
                                }
                                if (newTabStop == null)
                                {
                                    newTabStop = GetLastFocusableElement(parent, null);
                                }
                                break;
                            }

                            parent = (_contentRoot as Visual)?.VisualRoot as IInputElement;
                        }
                        else if (parentElement is InputElement pIE && KeyboardNavigation.GetTabNavigation(pIE) == KeyboardNavigationMode.None)
                        {
                            if (FocusHelpers.IsFocusable(parent))
                            {
                                newTabStop = parent;
                            }
                            else
                            {
                                current = parent;
                                parent = FocusHelpers.GetFocusParent(focused);
                                if (parent == null)
                                    break;
                            }
                        }
                        else
                        {
                            parent = parentElement as IInputElement;
                        }
                    }

                    newTabStop = GetNextOrPreviousTabStopInternal(parent, current, newTabStop, false, ref currentPassed, ref currentCompare);

                    if (newTabStop == null && FocusHelpers.IsPotentialTabStop(parent) && FocusHelpers.IsFocusable(parent))
                    {
                        if (parent is InputElement iE && KeyboardNavigation.GetTabNavigation(iE) == KeyboardNavigationMode.Cycle)
                        {
                            newTabStop = GetLastFocusableElement(parent, null);
                        }
                        else
                        {
                            newTabStop = parent;
                        }
                    }
                    else
                    {
                        if (newTabStop != null && FocusHelpers.CanHaveFocusableChildren(newTabStop as AvaloniaObject))
                        {
                            newTabStop = GetLastFocusableElement(newTabStop, null);
                        }
                    }

                    if (newTabStop != null)
                        break;

                    if (IsValidTabStopSearchCandidate(parent))
                    {
                        current = parent;
                    }

                    parent = FocusHelpers.GetFocusParent(parent);
                    currentPassed = false;
                }
            }

            return newTabStop;
        }

        private IInputElement? GetNextOrPreviousTabStopInternal(IInputElement? parent, IInputElement? current, IInputElement? candidate, bool findNext, ref bool currentPassed, ref IInputElement? currentCompare)
        {
            var newTabStop = candidate;
            IInputElement? childStop = null;
            int compareIndexResult = 0;
            bool compareCurrentForPreviousElement = false;

            if (IsValidTabStopSearchCandidate(current))
            {
                currentCompare = current;
            }

            if (parent != null)
            {
                bool foundCurrent = false;
                foreach (var child in FocusHelpers.GetInputElementChildren(parent as AvaloniaObject))
                {
                    childStop = null;
                    compareCurrentForPreviousElement = false;
                    if (child == current)
                    {
                        foundCurrent = true;
                        currentPassed = true;
                        continue;
                    }

                    if (FocusHelpers.IsVisible(child))
                    {
                        if (child == current)
                        {
                            foundCurrent = true;
                            currentPassed = true;
                            continue;
                        }

                        if (IsValidTabStopSearchCandidate(child))
                        {
                            if (!FocusHelpers.IsPotentialTabStop(child))
                            {
                                childStop = GetNextOrPreviousTabStopInternal(childStop, current, newTabStop, findNext, ref currentPassed, ref currentCompare);
                                compareCurrentForPreviousElement = true;
                            }
                            else
                            {
                                childStop = child;
                            }
                        }
                        else if (FocusHelpers.CanHaveFocusableChildren(child as AvaloniaObject))
                        {
                            childStop = GetNextOrPreviousTabStopInternal(child, current, newTabStop, findNext, ref currentPassed, ref currentCompare);
                            compareCurrentForPreviousElement = true;
                        }
                    }

                    if (childStop != null && (FocusHelpers.IsFocusable(childStop) || FocusHelpers.CanHaveFocusableChildren(childStop as AvaloniaObject)))
                    {
                        compareIndexResult = CompareTabIndex(childStop, currentCompare);

                        if (findNext)
                        {
                            if (compareIndexResult > 0 || ((foundCurrent || currentPassed) && compareIndexResult == 0))
                            {
                                if (newTabStop != null)
                                {
                                    if (CompareTabIndex(childStop, newTabStop) < 0)
                                    {
                                        newTabStop = childStop;
                                    }
                                }
                                else
                                {
                                    newTabStop = childStop;
                                }
                            }
                        }
                        else
                        {
                            if (compareIndexResult < 0 || (((foundCurrent || currentPassed) || compareCurrentForPreviousElement) && compareIndexResult == 0))
                            {
                                if (newTabStop != null)
                                {
                                    if (CompareTabIndex(childStop, newTabStop) >= 0)
                                    {
                                        newTabStop = childStop;
                                    }
                                }
                                else
                                {
                                    newTabStop = childStop;
                                }
                            }
                        }
                    }
                }
            }

            return newTabStop;
        }

        private static int CompareTabIndex(IInputElement? control1, IInputElement? control2)
        {
            return GetTabIndex(control1).CompareTo(GetTabIndex(control2));
        }

        private static int GetTabIndex(IInputElement? element)
        {
            if (element is InputElement inputElement)
                return inputElement.TabIndex;

            return int.MaxValue;
        }

        private bool CanProcessTabStop(bool isReverse)
        {
            bool isFocusOnFirst = false;
            bool isFocusOnLast = false;
            bool canProcessTab = true;
            if (IsFocusedElementInPopup())
            {
                return true;
            }

            if (isReverse)
            {
                isFocusOnFirst = IsFocusOnFirstTabStop();
            }
            else
            {
                isFocusOnLast = IsFocusOnLastTabStop();
            }

            if (isFocusOnFirst || isFocusOnLast)
            {
                canProcessTab = false;
            }

            if (canProcessTab)
            {
                var edge = GetFirstFocusableElementFromRoot(!isReverse);

                if (edge != null)
                {
                    var edgeParent = GetParentTabStopElement(edge);
                    if (edgeParent is InputElement inputElement && KeyboardNavigation.GetTabNavigation(inputElement) == KeyboardNavigationMode.Once && edgeParent == GetParentTabStopElement(Current))
                    {
                        canProcessTab = false;
                    }
                }
                else
                {
                    canProcessTab = false;
                }
            }
            else
            {
                if (isFocusOnLast || isFocusOnFirst)
                {
                    if (Current is InputElement inputElement && KeyboardNavigation.GetTabNavigation(inputElement) == KeyboardNavigationMode.Cycle)
                    {
                        canProcessTab = true;
                    }
                    else
                    {
                        var focusedParent = GetParentTabStopElement(Current);
                        while (focusedParent != null)
                        {
                            if (focusedParent is InputElement iE && KeyboardNavigation.GetTabNavigation(iE) == KeyboardNavigationMode.Cycle)
                            {
                                canProcessTab = true;
                                break;
                            }

                            focusedParent = GetParentTabStopElement(focusedParent as IInputElement);
                        }
                    }
                }
            }

            return canProcessTab;
        }

        private AvaloniaObject? GetParentTabStopElement(IInputElement? current)
        {
            if (current != null)
            {
                var parent = FocusHelpers.GetFocusParent(current);

                while (parent != null)
                {
                    if (IsValidTabStopSearchCandidate(parent) && parent is InputElement element)
                    {
                        return element;
                    }

                    parent = FocusHelpers.GetFocusParent(parent);
                }
            }

            return null;
        }

        private bool IsValidTabStopSearchCandidate(IInputElement? element)
        {
            var isValid = FocusHelpers.IsPotentialTabStop(element);

            if (!isValid)
            {
                isValid = (element as InputElement)?.IsSet(KeyboardNavigation.TabNavigationProperty) ?? false;
            }

            return isValid;
        }

        private IInputElement? GetFirstFocusableElementFromRoot(bool isReverse)
        {
            var root = (_contentRoot as Visual)?.VisualRoot as IInputElement;

            if (root != null)
                return !isReverse ? GetFirstFocusableElement(root, null) : GetLastFocusableElement(root, null);

            return null;
        }

        private bool IsFocusOnLastTabStop()
        {
            if (Current == null || _contentRoot is not Visual visual)
                return false;
            var root = visual.VisualRoot as IInputElement;

            Debug.Assert(root != null);

            var lastFocus = GetLastFocusableElement(root, null);

            return lastFocus == Current;
        }

        private bool IsFocusOnFirstTabStop()
        {
            if (Current == null || _contentRoot is not Visual visual)
                return false;
            var root = visual.VisualRoot as IInputElement;

            Debug.Assert(root != null);

            var firstFocus = GetFirstFocusableElement(root, null);

            return firstFocus == Current;
        }

        private static IInputElement? GetFirstFocusableElement(IInputElement searchStart, IInputElement? firstFocus = null)
        {
            firstFocus = GetFirstFocusableElementInternal(searchStart, firstFocus);

            if (firstFocus != null && !firstFocus.Focusable && FocusHelpers.CanHaveFocusableChildren(firstFocus as AvaloniaObject))
            {
                firstFocus = GetFirstFocusableElement(firstFocus, null);
            }

            return firstFocus;
        }

        private IInputElement? GetLastFocusableElement(IInputElement searchStart, IInputElement? lastFocus = null)
        {
            lastFocus = GetLastFocusableElementInternal(searchStart, lastFocus);

            if (lastFocus != null && !lastFocus.Focusable && FocusHelpers.CanHaveFocusableChildren(lastFocus as AvaloniaObject))
            {
                lastFocus = GetLastFocusableElement(lastFocus, null);
            }

            return lastFocus;
        }

        private bool IsFocusedElementInPopup() => Current != null && GetRootOfPopupSubTree(Current) != null;

        private Visual? GetRootOfPopupSubTree(IInputElement? current)
        {
            //TODO Popup api
            return null;
        }

        private bool FindAndSetNextFocus(NavigationDirection direction, XYFocusOptions xYFocusOptions)
        {
            var focusChanged = false;
            if (xYFocusOptions.UpdateManifoldsFromFocusHintRect && xYFocusOptions.FocusHintRectangle != null)
            {
                _xyFocus.SetManifoldsFromBounds(xYFocusOptions.FocusHintRectangle ?? default);
            }

            if (FindNextFocus(direction, xYFocusOptions, false) is { } nextFocusedElement)
            {
                focusChanged = nextFocusedElement.Focus();

                if (focusChanged && xYFocusOptions.UpdateManifold && nextFocusedElement is InputElement inputElement)
                {
                    var bounds = xYFocusOptions.FocusHintRectangle ?? xYFocusOptions.FocusedElementBounds ?? default;

                    _xyFocus.UpdateManifolds(direction, bounds, inputElement, xYFocusOptions.IgnoreClipping);
                }
            }

            return focusChanged;

        }
    }
}
