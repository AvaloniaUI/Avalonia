using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using Avalonia.Input.Navigation;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.Reactive;
using Avalonia.Rendering;
using Avalonia.VisualTree;
using static Avalonia.Rendering.Composition.Expressions.ExpressionTrackedObjects;

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

        public FocusManager(IInputElement? contentRoot = null)
        {
            _contentRoot = contentRoot;
        }

        private IInputElement? Current => KeyboardDevice.Instance?.FocusedElement;

        private XYFocus _xyFocus = new();
        private bool _canTabOutOfPlugin = false;
        private bool _isMovingFocusToPreviousTabStop;
        private bool _isMovingFocusToNextTabStop;
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

        public bool TryMoveFocus(NavigationDirection direction)
        {
            return FindAndSetNextFocus(direction, new XYFocusOptions());
        }

        public bool TryMoveFocus(NavigationDirection direction, FindNextElementOptions findNextElementOptions)
        {
            if (direction is not NavigationDirection.Up
                and not NavigationDirection.Down
                and not NavigationDirection.Left
                and not NavigationDirection.Right)
            {
                throw new ArgumentOutOfRangeException(
                        $"{direction} is not supported with FindNextElementOptions. Only Up, Down, Left and right are supported");
            }

            var xyOption = new XYFocusOptions()
            {
                UpdateManifold = false,
                SearchRoot = findNextElementOptions.SearchRoot,
            };


            if (!findNextElementOptions.ExclusionRect.IsUniform())
                xyOption.ExclusionRect = findNextElementOptions.ExclusionRect;

            if (findNextElementOptions.FocusHintRectangle is { } rect && !rect.IsUniform())
                xyOption.FocusHintRectangle = findNextElementOptions.FocusHintRectangle;

            xyOption.NavigationStrategyOverride = findNextElementOptions.NavigationStrategyOverride;
            xyOption.IgnoreOcclusivity = findNextElementOptions.IgnoreOcclusivity;

            return FindAndSetNextFocus(direction, xyOption);
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

        public IInputElement? FindFirstFocusableElement()
        {
            var root = (_contentRoot as Visual)?.GetSelfAndVisualDescendants().FirstOrDefault(x => x is IInputElement) as IInputElement;
            if (root == null)
                return null;
            return GetFirstFocusableElementFromRoot(false);
        }

        public static IInputElement? FindFirstFocusableElement(IInputElement searchScope)
        {
            var focusManager = GetFocusManager(searchScope);

            if (focusManager != null)
            {
                return focusManager.GetFirstFocusableElement(searchScope);
            }
            else
                return null;
        }

        public IInputElement? FindLastFocusableElement()
        {
            var root = (_contentRoot as Visual)?.GetSelfAndVisualDescendants().FirstOrDefault(x => x is IInputElement) as IInputElement;
            if (root == null)
                return null;
            return GetFirstFocusableElementFromRoot(false);
        }

        public static IInputElement? FindLastFocusableElement(IInputElement searchScope)
        {
            var focusManager = GetFocusManager(searchScope);

            if (focusManager != null)
            {
                return focusManager.GetLastFocusableElement(searchScope);
            }
            else
                return null;
        }

        public IInputElement? FindNextElement(NavigationDirection focusNavigationDirection)
        {
            var xyOption = new XYFocusOptions()
            {
                UpdateManifold = false
            };

            return FindNextFocus(focusNavigationDirection, xyOption);
        }

        public IInputElement? FindNextElement(NavigationDirection focusNavigationDirection, FindNextElementOptions findNextElementOptions)
        {
            if (focusNavigationDirection is not NavigationDirection.Up
                and not NavigationDirection.Down
                and not NavigationDirection.Left
                and not NavigationDirection.Right)
            {
                throw new ArgumentOutOfRangeException(
                        $"{focusNavigationDirection} is not supported with FindNextElementOptions. Only Up, Down, Left and right are supported");
            }

            var xyOption = new XYFocusOptions()
            {
                UpdateManifold = false,
                SearchRoot = findNextElementOptions.SearchRoot,
            };


            if (!findNextElementOptions.ExclusionRect.IsUniform())
                xyOption.ExclusionRect = findNextElementOptions.ExclusionRect;

            if (findNextElementOptions.FocusHintRectangle is { } rect && !rect.IsUniform())
                xyOption.FocusHintRectangle = findNextElementOptions.FocusHintRectangle;

            xyOption.NavigationStrategyOverride = findNextElementOptions.NavigationStrategyOverride;
            xyOption.IgnoreOcclusivity = findNextElementOptions.IgnoreOcclusivity;

            return FindNextFocus(focusNavigationDirection, xyOption);
        }

        internal IInputElement? FindNextFocus(NavigationDirection direction, XYFocusOptions focusOptions, bool updateManifolds = true, bool isQueryOnly = true)
        {
            IInputElement? nextFocusedElement = null;

            var currentlyFocusedElement = Current;

            if (direction is NavigationDirection.Previous or NavigationDirection.Next || currentlyFocusedElement == null)
            {
                var isReverse = direction == NavigationDirection.Previous;
                if (isQueryOnly)
                {
                    nextFocusedElement = ProcessTabStopInternal(isReverse, true);
                }
                else
                {
                    using var _ = Disposable.Create(() =>
                    {
                        if (isReverse)
                        {
                            _isMovingFocusToPreviousTabStop = false;
                        }
                        else if (direction == NavigationDirection.Next)
                        {
                            _isMovingFocusToNextTabStop = false;
                        }
                    });

                    if (isReverse)
                    {
                        _isMovingFocusToPreviousTabStop = false;
                    }
                    else if (direction == NavigationDirection.Next)
                    {
                        _isMovingFocusToNextTabStop = false;
                    }
                    nextFocusedElement = ProcessTabStopInternal(isReverse, true);
                }
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
                    null, // todo: engaged control
                    updateManifolds,
                    focusOptions);
            }

            return nextFocusedElement;
        }

        internal IInputElement? GetFirstFocusableElementInternal(IInputElement searchStart, IInputElement? focusCandidate = null)
        {
            var children = FocusHelpers.GetFocusChildren(searchStart as AvaloniaObject);

            foreach (var child in children)
            {
                if (child is not null)
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


        internal IInputElement? GetLastFocusableElementInternal(IInputElement searchStart, IInputElement? lastFocus = null)
        {
            var children = FocusHelpers.GetFocusChildren(searchStart as AvaloniaObject);

            foreach (var child in children)
            {
                if (child is not null)
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
            bool isTabStopOverriden = false;

            var defaultCandidateTabStop = GetTabStopCandidateElement(isReverse, queryOnly, out var didCycleFocusAtRootVisualScope);

            //todo implement tab stop override

            /*if(isTabStopOverriden)
            {
                newTabStopElement
            }*/

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

            if (Current != null && _canTabOutOfPlugin)
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

                if (newTabStop == null && (!_canTabOutOfPlugin || internalCycleWorkaround || queryOnly))
                {
                    newTabStop = GetFirstFocusableElement(root, null);

                    didCycleFocusAtRootVisualScope = true;
                }
            }
            else
            {
                newTabStop = GetPreviousTabStop();

                if (newTabStop == null && (!_canTabOutOfPlugin || internalCycleWorkaround || queryOnly))
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
            IInputElement? newTabStop = null;
            IInputElement? currentCompare = focused;
            //IInputElement? newTabStopFromCallback = null;

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
                        if (current == GetParentElement(focused))
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
                        var parentElement = GetParentElement(parent);
                        if (parentElement == null)
                        {
                            parent = GetRootOfPopupSubTree(current) as IInputElement;

                            if (parent != null)
                            {
                                newTabStop = GetNextTabStopInternal(parent, current, newTabStop, ref currentPassed, ref currentCompare);

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
                            current = parentElement as IInputElement;
                            parent = FocusHelpers.GetFocusParent(current);
                            if (parent == null)
                                break;
                        }
                        else
                        {
                            if (parentElement != null)
                            {
                                parent = parentElement as IInputElement;
                            }
                            else
                            {
                                parent = (_contentRoot as Visual)?.VisualRoot as IInputElement;
                            }
                        }
                    }

                    newTabStop = GetNextTabStopInternal(parent, current, newTabStop, ref currentPassed, ref currentCompare);

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
            IInputElement? newTabStop = null;
            IInputElement? currentCompare = focused;
            //IInputElement? newTabStopFromCallback = null;

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
                        var parentElement = GetParentElement(parent);
                        if (parentElement == null)
                        {
                            parent = GetRootOfPopupSubTree(current) as IInputElement;

                            if (parent != null)
                            {
                                newTabStop = GetPreviousTabStopInternal(parent, current, newTabStop, ref currentPassed, ref currentCompare);

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
                            if (parentElement != null)
                            {
                                parent = parentElement as IInputElement;
                            }
                            else
                            {
                                parent = (_contentRoot as Visual)?.VisualRoot as IInputElement;
                            }
                        }
                    }

                    newTabStop = GetPreviousTabStopInternal(parent, current, newTabStop, ref currentPassed, ref currentCompare);

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

        private IInputElement? GetNextTabStopInternal(IInputElement? parent, IInputElement? current, IInputElement? candidate, ref bool currentPassed, ref IInputElement? currentCompare)
        {
            var newTabStop = candidate;
            IInputElement? childStop = null;
            int compareIndexResult = 0;

            if (IsValidTabStopSearchCandidate(current))
            {
                currentCompare = current;
            }

            if (parent != null)
            {
                bool foundCurrent = false;
                foreach (var child in FocusHelpers.GetFocusChildren(parent as AvaloniaObject))
                {
                    childStop = null;
                    if (child != null && child == current)
                    {
                        foundCurrent = true;
                        currentPassed = true;
                        continue;
                    }

                    if (child != null && FocusHelpers.IsVisible(child))
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
                                childStop = GetNextTabStopInternal(childStop, current, newTabStop, ref currentPassed, ref currentCompare);
                            }
                            else
                            {
                                childStop = child;
                            }
                        }
                        else if (FocusHelpers.CanHaveFocusableChildren(child as AvaloniaObject))
                        {
                            childStop = GetNextTabStopInternal(child, current, newTabStop, ref currentPassed, ref currentCompare);
                        }
                    }

                    if (childStop != null && (FocusHelpers.IsFocusable(childStop) || FocusHelpers.CanHaveFocusableChildren(childStop as AvaloniaObject)))
                    {
                        compareIndexResult = CompareTabIndex(childStop, currentCompare);

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
                }
            }

            return newTabStop;
        }

        private IInputElement? GetPreviousTabStopInternal(IInputElement? parent, IInputElement? current, IInputElement? candidate, ref bool currentPassed, ref IInputElement? currentCompare)
        {
            var newTabStop = candidate;
            IInputElement? childStop = null;

            if (IsValidTabStopSearchCandidate(current))
            {
                currentCompare = current;
            }

            if (parent != null)
            {
                int compareIndexResult = 0;
                bool foundCurrent = false;
                bool bCurrentCompare = false;

                foreach (var child in FocusHelpers.GetFocusChildren(parent as AvaloniaObject))
                {
                    childStop = null;
                    bCurrentCompare = false;
                    if (child != null && child == current)
                    {
                        foundCurrent = true;
                        currentPassed = true;
                        continue;
                    }

                    if (child != null && FocusHelpers.IsVisible(child))
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
                                childStop = GetPreviousTabStopInternal(childStop, current, newTabStop, ref currentPassed, ref currentCompare);
                                bCurrentCompare = true;
                            }
                            else
                            {
                                childStop = child;
                            }
                        }
                        else if (FocusHelpers.CanHaveFocusableChildren(child as AvaloniaObject))
                        {
                            childStop = GetPreviousTabStopInternal(child, current, newTabStop, ref currentPassed, ref currentCompare);
                            bCurrentCompare = true;
                        }
                    }

                    if (childStop != null && (FocusHelpers.IsFocusable(childStop) || FocusHelpers.CanHaveFocusableChildren(childStop as AvaloniaObject)))
                    {
                        compareIndexResult = CompareTabIndex(childStop, currentCompare);

                        if (compareIndexResult < 0 || (((foundCurrent || currentPassed) || bCurrentCompare) && compareIndexResult == 0))
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

            return newTabStop;
        }

        private static int CompareTabIndex(IInputElement? control1, IInputElement? control2)
        {
            if (GetTabIndex(control1) > GetTabIndex(control2))
                return 1;
            else if (GetTabIndex(control1) < GetTabIndex(control2))
                return -1;

            return 0;
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
                    var edgeParent = GetParentElement(edge);
                    if (edgeParent is InputElement inputElement && KeyboardNavigation.GetTabNavigation(inputElement) == KeyboardNavigationMode.Once && edgeParent == GetParentElement(Current))
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
                        var focusedParent = GetParentElement(Current);
                        while (focusedParent != null)
                        {
                            if (focusedParent is InputElement iE && KeyboardNavigation.GetTabNavigation(iE) == KeyboardNavigationMode.Cycle)
                            {
                                canProcessTab = true;
                                break;
                            }

                            focusedParent = GetParentElement(focusedParent as IInputElement);
                        }
                    }
                }
            }

            return canProcessTab;
        }

        private AvaloniaObject? GetParentElement(IInputElement? current)
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
                return isReverse ? GetFirstFocusableElement(root, null) : GetLastFocusableElement(root, null);

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

        private IInputElement? GetFirstFocusableElement(IInputElement searchStart, IInputElement? firstFocus = null)
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

            var queryOnly = false;

            if (FindNextFocus(direction, xYFocusOptions, false, queryOnly) is { } nextFocusedElement)
            {
                focusChanged = nextFocusedElement.Focus();

                if(focusChanged && xYFocusOptions.UpdateManifold && nextFocusedElement is InputElement inputElement)
                {
                    var bounds = xYFocusOptions.FocusHintRectangle ?? xYFocusOptions.FocusedElementBounds ?? default;

                    _xyFocus.UpdateManifolds(direction, bounds, inputElement, xYFocusOptions.IgnoreClipping);
                }
            }

            return focusChanged;

        }
    }
}
