using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input.Navigation;
using Avalonia.Interactivity;
using Avalonia.Metadata;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Manages focus for the application.
    /// </summary>
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

        [PrivateApi]
        public FocusManager()
        {
        }

        /// <summary>
        /// Gets or sets the content root for the focus management system.
        /// </summary>
        [PrivateApi]
        public IInputElement? ContentRoot
        {
            get => _contentRoot;
            set => _contentRoot = value;
        }

        private IInputElement? Current => KeyboardDevice.Instance?.FocusedElement;

        private readonly XYFocus _xyFocus = new();
        private IInputElement? _contentRoot;
        private XYFocusOptions? _reusableFocusOptions;

        /// <inheritdoc />
        public IInputElement? GetFocusedElement() => Current;

        /// <inheritdoc />
        public bool Focus(
            IInputElement? element,
            NavigationMethod method = NavigationMethod.Unspecified,
            KeyModifiers keyModifiers = KeyModifiers.None)
        {
            if (KeyboardDevice.Instance is not { } keyboardDevice)
                return false;

            if (element is not null)
            {
                return FocusCore(keyboardDevice, element, method, keyModifiers);
            }

            if (_focusRoot?.GetValue(FocusedElementProperty) is { } restore && restore != Current)
            {
                if (!CanFocus(restore))
                {
                    // Previous effective focus is no longer part of the focus root's visual tree. We clear the focused element
                    _focusRoot.ClearValue(FocusedElementProperty);

                    // Keep focus in another scope (e.g. window), unless the focused element was removed or hidden.
                    if (Current != null && IsVisible(Current) && GetFocusScope(Current) != _focusRoot)
                    {
                        _focusRoot = null;

                        return false;
                    }
                }
                else
                {
                    return FocusCore(keyboardDevice, restore, method, keyModifiers);
                }
            }

            _focusRoot = null;
            keyboardDevice.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, false);
            return false;
        }

        private bool FocusCore(
            KeyboardDevice keyboardDevice,
            IInputElement element,
            NavigationMethod method,
            KeyModifiers keyModifiers)
        {
            if (!CanFocus(element))
                return false;

            keyboardDevice.SetFocusedElement(element, method, keyModifiers);

            if (keyboardDevice.FocusedElement is { } effectivelyFocusedElement)
            {
                if (GetFocusScope(effectivelyFocusedElement) is { } scope)
                {
                    scope.SetValue(FocusedElementProperty, effectivelyFocusedElement);
                    _focusRoot = GetFocusRoot(scope);
                }

                return effectivelyFocusedElement == element;
            }

            _focusRoot = null;
            keyboardDevice.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, false);
            return false;
        }

        internal void ClearFocusOnElementRemoved(IInputElement removedElement, Visual oldParent)
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

        [PrivateApi]
        public IInputElement? GetFocusedElement(IFocusScope scope)
        {
            return (scope as StyledElement)?.GetValue(FocusedElementProperty);
        }

        /// <summary>
        /// Notifies the focus manager of a change in focus scope.
        /// </summary>
        /// <param name="scope">The new focus scope.</param>
        [PrivateApi]
        public void SetFocusScope(IFocusScope scope)
        {
            if (KeyboardDevice.Instance is not { } keyboardDevice)
                return;

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
            else
            {
                // If the scope isn't focusable, make sure we still set it as the current focus root,
                // otherwise it will be completely ignored
                _focusRoot = scope as StyledElement;
                keyboardDevice.SetFocusedElement(null, NavigationMethod.Unspecified, KeyModifiers.None, false);
            }
        }

        [PrivateApi]
        public void RemoveFocusRoot(IFocusScope scope)
        {
            if (scope == _focusRoot)
                Focus(null);
        }

        [PrivateApi]
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
            return (FocusManager?)(element as Visual)?.GetInputRoot()?.FocusManager
                // In our unit tests some elements might not have a root. Remove when we migrate to headless tests.
                ?? (FocusManager?)AvaloniaLocator.Current.GetService<IFocusManager>();
        }

        /// <inheritdoc />
        public bool TryMoveFocus(NavigationDirection direction, FindNextElementOptions? options = null)
        {
            ValidateDirection(direction);

            var focusOptions = ToFocusOptions(options, true);
            var result = FindAndSetNextFocus(options?.FocusedElement ?? Current, direction, focusOptions);
            _reusableFocusOptions = focusOptions;
            return result;
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
                return ev switch
                {
                    PointerReleasedEventArgs releasedEventArgs when releasedEventArgs.Pointer.Type != PointerType.Mouse => true,
                    PointerPressedEventArgs pressedEventArgs when pressedEventArgs.Pointer.Type == PointerType.Mouse => true,
                    _ => false,
                };
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

            var root = v.PresentationSource?.InputRoot.FocusRoot as Visual;

            while (root is IHostedVisualTreeRoot hosted &&
                hosted.Host?.PresentationSource?.InputRoot.FocusRoot is { } parentRoot)
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public IInputElement? FindNextElement(NavigationDirection direction, FindNextElementOptions? options = null)
        {
            ValidateDirection(direction);

            var focusOptions = ToFocusOptions(options, false);
            var result = FindNextFocus(options?.FocusedElement ?? Current, direction, focusOptions);
            _reusableFocusOptions = focusOptions;
            return result;
        }

        private static void ValidateDirection(NavigationDirection direction)
        {
            if (direction is not (
                NavigationDirection.Next or
                NavigationDirection.Previous or
                NavigationDirection.Up or
                NavigationDirection.Down or
                NavigationDirection.Left or
                NavigationDirection.Right))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(direction),
                    direction,
                    $"Only {nameof(NavigationDirection.Next)}, {nameof(NavigationDirection.Previous)}, " +
                    $"{nameof(NavigationDirection.Up)}, {nameof(NavigationDirection.Down)}," +
                    $" {nameof(NavigationDirection.Left)} and {nameof(NavigationDirection.Right)} directions are supported");
            }
        }

        private XYFocusOptions ToFocusOptions(FindNextElementOptions? options, bool updateManifold)
        {
            // XYFocus only uses the options and never modifies them; we can cache and reset them between calls.
            var focusOptions = _reusableFocusOptions;
            _reusableFocusOptions = null;

            if (focusOptions is null)
                focusOptions = new XYFocusOptions();
            else
                focusOptions.Reset();

            if (options is not null)
            {
                focusOptions.SearchRoot = options.SearchRoot;
                focusOptions.ExclusionRect = options.ExclusionRect;
                focusOptions.FocusHintRectangle = options.FocusHintRectangle;
                focusOptions.NavigationStrategyOverride = options.NavigationStrategyOverride;
                focusOptions.IgnoreOcclusivity = options.IgnoreOcclusivity;
            }

            focusOptions.UpdateManifold = updateManifold;

            return focusOptions;
        }

        private IInputElement? FindNextFocus(
            IInputElement? focusedElement,
            NavigationDirection direction,
            XYFocusOptions focusOptions,
            bool updateManifolds = true)
        {
            IInputElement? nextFocusedElement;

            if (direction is NavigationDirection.Previous or NavigationDirection.Next || focusedElement == null)
            {
                var isReverse = direction == NavigationDirection.Previous;
                nextFocusedElement = ProcessTabStopInternal(focusedElement, isReverse);
            }
            else
            {
                if (focusedElement is InputElement inputElement &&
                    XYFocus.GetBoundsForRanking(inputElement, focusOptions.IgnoreClipping) is { } bounds)
                {
                    focusOptions.FocusedElementBounds = bounds;
                }

                nextFocusedElement = _xyFocus.GetNextFocusableElement(direction,
                    focusedElement as InputElement,
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

        private IInputElement? ProcessTabStopInternal(IInputElement? focusedElement, bool isReverse)
        {
            var candidate = GetTabStopCandidateElement(focusedElement, isReverse, out var didCycleFocusAtRootVisualScope);
            return ApplyTabStopOverrides(_contentRoot, focusedElement, candidate, isReverse, didCycleFocusAtRootVisualScope);
        }

        private static IInputElement? ApplyTabStopOverrides(
            IInputElement? contentRoot,
            IInputElement? focusedElement,
            IInputElement? candidate,
            bool isReverse,
            bool didCycleFocusAtRootVisualScope)
        {
            var isTabStopOverriden = InputElement.ProcessTabStop(contentRoot,
                focusedElement,
                candidate,
                isReverse,
                didCycleFocusAtRootVisualScope,
                out var newTabStopFromCallback);

            return isTabStopOverriden ? newTabStopFromCallback : candidate;
        }

        private IInputElement? GetTabStopCandidateElement(
            IInputElement? focusedElement,
            bool isReverse,
            out bool didCycleFocusAtRootVisualScope)
        {
            didCycleFocusAtRootVisualScope = false;
            var root = _contentRoot;

            if (root == null)
                return null;

            if (focusedElement != null)
            {
                var candidate = FindTabStopFrom(focusedElement, isReverse, ref didCycleFocusAtRootVisualScope);

                if (candidate != null || IsInContainedTabGroup(focusedElement))
                    return candidate;
            }

            // This API always wraps at the root, unlike the Tab key, which leaves a root that isn't Cycle.
            didCycleFocusAtRootVisualScope = true;
            var ignored = false;

            return isReverse ?
                GetPreviousTabStop(null, root, true, ref ignored) :
                GetNextTabStop(root, root, true, ref ignored);
        }

        /// <summary>
        /// Finds the element that the Tab key should focus next.
        /// </summary>
        /// <param name="element">The focused element, or the root when nothing is focused.</param>
        /// <param name="isReverse">Whether to search backwards.</param>
        /// <returns>The element, or null when navigation stops at the edge of a group or root.</returns>
        internal static IInputElement? FindNextTabStop(IInputElement element, bool isReverse)
        {
            var didCycle = false;
            var candidate = FindTabStopFrom(element, isReverse, ref didCycle);
            return ApplyTabStopOverrides(null, element, candidate, isReverse, didCycle);
        }

        /// <summary>
        /// Finds the tab stop that follows all of the content of a container.
        /// </summary>
        internal static IInputElement? FindNextTabStopAfter(IInputElement container)
        {
            var didCycle = false;
            var last = GetLastInTabOrderTree(container);
            return GetNextTabStop(last, GetTabGroupParent(last), false, ref didCycle);
        }

        /// <summary>
        /// Finds the tab stop that precedes all of the content of a container.
        /// </summary>
        internal static IInputElement? FindPreviousTabStopBefore(IInputElement container)
        {
            if (GetFirstAvailableChild(container) is not { } first)
                return null;

            var didCycle = false;
            return GetPreviousTabStop(first, null, false, ref didCycle);
        }

        private static IInputElement? FindTabStopFrom(IInputElement element, bool isReverse, ref bool didCycle)
        {
            var inputElement = element as InputElement;

            return isReverse ?
                inputElement?.GetPreviousTabStopOverride() ?? GetPreviousTabStop(element, null, false, ref didCycle) :
                inputElement?.GetNextTabStopOverride() ?? GetNextTabStop(element, GetTabGroupParent(element), false, ref didCycle);
        }

        private static IInputElement? GetNextTabStop(
            IInputElement? element,
            IInputElement container,
            bool goDownOnly,
            ref bool didCycle)
        {
            var mode = GetTabNavigationMode(container);

            if (element == null)
            {
                if (IsTabStop(container))
                    return container;

                if (GetEntryOverride(container, false) is { } entryOverride &&
                    GetNextTabStop(null, entryOverride, true, ref didCycle) is { } entryTabStop)
                {
                    return entryTabStop;
                }

                if (GetActiveElement(container) is { } activeElement)
                    return GetNextTabStop(null, activeElement, true, ref didCycle);
            }
            else if (mode is KeyboardNavigationMode.Once or KeyboardNavigationMode.None && container != element)
            {
                return goDownOnly ?
                    null :
                    GetNextTabStop(container, GetTabGroupParent(container), false, ref didCycle);
            }

            if (mode != KeyboardNavigationMode.None)
            {
                var members = GetTabGroupMembers(container, element, false);
                var index = element == null || element == container ? -1 : members.IndexOf(element);
                var steps = mode == KeyboardNavigationMode.Cycle ? members.Count : members.Count - index - 1;

                for (var step = 1; step <= steps; step++)
                {
                    var next = members[(index + step) % members.Count];

                    if (next == element && !IsTabStopOrGroup(next))
                        continue;

                    if (index + step >= members.Count && GetTabParent(container) == null)
                        didCycle = true;

                    if (GetNextTabStop(null, next, true, ref didCycle) is { } inside)
                        return inside;

                    // Once only limits entering the group: an empty first member shouldn't make it unreachable,
                    // but having tried one shouldn't let the search leave a group it was asked to enter.
                    if (mode == KeyboardNavigationMode.Once)
                        mode = KeyboardNavigationMode.Contained;
                }
            }

            if (!goDownOnly && mode != KeyboardNavigationMode.Contained && GetTabParent(container) != null)
                return GetNextTabStop(container, GetTabGroupParent(container), false, ref didCycle);

            return null;
        }

        private static IInputElement? GetPreviousTabStop(
            IInputElement? element,
            IInputElement? container,
            bool goDownOnly,
            ref bool didCycle)
        {
            container ??= GetTabGroupParent(
                element ?? throw new InvalidOperationException("Either 'element' or 'container' must be non-null."));

            var mode = GetTabNavigationMode(container);

            if (element == null)
            {
                if (GetEntryOverride(container, true) is { } entryOverride &&
                    GetPreviousTabStop(null, entryOverride, true, ref didCycle) is { } entryTabStop)
                {
                    return entryTabStop;
                }

                if (GetActiveElement(container) is { } activeElement)
                    return GetPreviousTabStop(null, activeElement, true, ref didCycle);

                // A Once group is a single stop, so entering it backwards still lands on its first item.
                if (mode == KeyboardNavigationMode.Once)
                {
                    var members = GetTabGroupMembers(container, null, true);

                    if (members.Count > 0)
                        return GetPreviousTabStop(null, members[0], true, ref didCycle);

                    if (IsTabStop(container))
                        return container;

                    return goDownOnly ? null : GetPreviousTabStop(container, null, false, ref didCycle);
                }
            }
            else if (mode is KeyboardNavigationMode.Once or KeyboardNavigationMode.None)
            {
                if (goDownOnly || container == element)
                    return null;

                return IsTabStop(container) ? container : GetPreviousTabStop(container, null, false, ref didCycle);
            }

            if (mode != KeyboardNavigationMode.None && element != container)
            {
                var members = GetTabGroupMembers(container, element, true);
                var index = element == null ? members.Count : members.IndexOf(element);
                var steps = mode == KeyboardNavigationMode.Cycle ? members.Count : index;

                for (var step = 1; step <= steps; step++)
                {
                    var previous = members[(index - step + members.Count) % members.Count];

                    if (previous == element && !IsTabStopOrGroup(previous))
                        continue;

                    if (index - step < 0 && GetTabParent(container) == null)
                        didCycle = true;

                    if (IsTabStop(previous) && !IsTabGroup(previous))
                        return previous;

                    if (GetPreviousTabStop(null, previous, true, ref didCycle) is { } inside)
                        return inside;
                }
            }

            if (mode == KeyboardNavigationMode.Contained)
                return null;

            if (element != container && IsTabStop(container))
                return container;

            if (!goDownOnly && GetTabParent(container) != null)
                return GetPreviousTabStop(container, null, false, ref didCycle);

            return null;
        }

        /// <summary>
        /// Gets the elements that take part in a group's tab order, sorted by that order.
        /// </summary>
        /// <remarks>
        /// Containers that aren't groups are transparent, so a TabIndex competes with every other one in the
        /// group, however deeply nested. The sort must be stable: elements with equal indices use tree order.
        /// </remarks>
        private static List<IInputElement> GetTabGroupMembers(IInputElement group, IInputElement? element, bool isReverse)
        {
            var members = new List<IInputElement>();
            CollectTabGroupMembers(group, element, isReverse, members);
            return members.OrderBy(GetTabIndex).ToList();
        }

        private static void CollectTabGroupMembers(
            IInputElement parent,
            IInputElement? element,
            bool isReverse,
            List<IInputElement> members)
        {
            foreach (var child in FocusHelpers.GetInputElementChildren(parent as AvaloniaObject))
            {
                var isAvailable = IsAvailableForTabbing(child);
                var isScope = IsTabGroup(child) || GetEntryOverride(child, isReverse) != null;

                // The starting element keeps its place even when it can't be tabbed to: it may have lost
                // that ability while focused, and the search still has to continue from where it is.
                if (child == element || (isAvailable && (isScope || IsTabStop(child))))
                    members.Add(child);

                if (!isScope && (isAvailable || (child as Visual)?.IsVisualAncestorOf(element as Visual) == true))
                    CollectTabGroupMembers(child, element, isReverse, members);
            }
        }

        private static IInputElement GetLastInTabOrderTree(IInputElement container)
        {
            var result = container;

            while (GetLastAvailableChild(result) is { } last)
            {
                if (IsTabGroup(last))
                    return last;

                result = last;
            }

            return result;
        }

        private static IInputElement? GetFirstAvailableChild(IInputElement element) =>
            IsAvailableForTabbing(element) ?
                FocusHelpers.GetInputElementChildren(element as AvaloniaObject).FirstOrDefault(IsAvailableForTabbing) :
                null;

        private static IInputElement? GetLastAvailableChild(IInputElement element) =>
            IsAvailableForTabbing(element) ?
                FocusHelpers.GetInputElementChildren(element as AvaloniaObject).LastOrDefault(IsAvailableForTabbing) :
                null;

        private static IInputElement? GetEntryOverride(IInputElement element, bool isReverse)
        {
            if (element is not InputElement inputElement)
                return null;

            return isReverse ?
                inputElement.GetLastFocusableElementOverride() :
                inputElement.GetFirstFocusableElementOverride();
        }

        private static IInputElement? GetActiveElement(IInputElement element) =>
            (element as InputElement)?.GetValue(KeyboardNavigation.TabOnceActiveElementProperty);

        private static IInputElement GetTabGroupParent(IInputElement element)
        {
            var result = element;

            while (GetTabParent(result) is { } parent)
            {
                if (IsTabGroup(parent))
                    return parent;

                result = parent;
            }

            return result;
        }

        private static IInputElement? GetTabParent(IInputElement element) =>
            (element as Visual)?.FindAncestorOfType<IInputElement>();

        private static bool IsInContainedTabGroup(IInputElement element)
        {
            for (var parent = GetTabParent(element); parent != null; parent = GetTabParent(parent))
            {
                if (GetTabNavigationMode(parent) == KeyboardNavigationMode.Contained)
                    return true;
            }

            return false;
        }

        private static KeyboardNavigationMode GetTabNavigationMode(IInputElement element) =>
            (element as InputElement)?.GetValue(KeyboardNavigation.TabNavigationProperty) ?? KeyboardNavigationMode.Continue;

        private static bool IsTabGroup(IInputElement element) =>
            GetTabNavigationMode(element) != KeyboardNavigationMode.Continue;

        private static bool IsTabStopOrGroup(IInputElement element) => IsTabStop(element) || IsTabGroup(element);

        // IsTabStop defaults to true on every InputElement, so it can't identify a stop without Focusable.
        private static bool IsTabStop(IInputElement element) =>
            element is InputElement { IsTabStop: true, Focusable: true } &&
            element.IsEffectivelyEnabled &&
            FocusHelpers.IsVisible(element);

        // Uses the element's own IsVisible so that a search starting inside a hidden subtree can still find
        // its way out of it. IsTabStop is what keeps the hidden elements themselves from being returned.
        private static bool IsAvailableForTabbing(IInputElement element) =>
            element.IsEffectivelyEnabled && element is Visual { IsVisible: true };

        private static int GetTabIndex(IInputElement? element)
        {
            if (element is InputElement inputElement)
                return inputElement.TabIndex;

            return int.MaxValue;
        }

        private IInputElement? GetFirstFocusableElementFromRoot(bool isReverse)
        {
            var root = (_contentRoot as Visual)?.VisualRoot as IInputElement;

            if (root != null)
                return !isReverse ? GetFirstFocusableElement(root, null) : GetLastFocusableElement(root, null);

            return null;
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

        private bool FindAndSetNextFocus(IInputElement? focusedElement, NavigationDirection direction, XYFocusOptions xYFocusOptions)
        {
            var focusChanged = false;
            if (xYFocusOptions.UpdateManifoldsFromFocusHintRect && xYFocusOptions.FocusHintRectangle != null)
            {
                _xyFocus.SetManifoldsFromBounds(xYFocusOptions.FocusHintRectangle ?? default);
            }

            if (FindNextFocus(focusedElement, direction, xYFocusOptions, false) is { } nextFocusedElement)
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
