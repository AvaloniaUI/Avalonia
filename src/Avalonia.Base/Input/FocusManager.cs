using System;
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

            // Unlike the Tab key, this API wraps at a root that isn't Cycle.
            didCycleFocusAtRootVisualScope = true;
            var ignored = false;

            return isReverse ?
                GetPreviousTabStop(null, root, true, ref ignored) :
                GetNextTabStop(root, root, true, ref ignored);
        }

        internal static IInputElement? FindNextTabStop(IInputElement element, bool isReverse)
        {
            var didCycle = false;
            var candidate = FindTabStopFrom(element, isReverse, ref didCycle);
            return ApplyTabStopOverrides(null, element, candidate, isReverse, didCycle);
        }

        internal static IInputElement? FindNextTabStopAfter(IInputElement container)
        {
            var didCycle = false;
            var last = GetLastInTabOrderTree(container);
            return GetNextTabStop(last, GetTabGroupParent(last), false, ref didCycle);
        }

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
                if (EnterTabGroupMembers(element, container, mode, false, ref didCycle) is { } inside)
                    return inside;

                // Keep searching inside a Once group, but don't leave it.
                if (mode == KeyboardNavigationMode.Once &&
                    FindNextTabOrderKey(container, long.MinValue, long.MaxValue, false, long.MaxValue) != long.MaxValue)
                {
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

                // Entering a Once group backwards still lands on its first item.
                if (mode == KeyboardNavigationMode.Once)
                {
                    var firstKey = FindNextTabOrderKey(container, long.MinValue, long.MaxValue, false, long.MaxValue);

                    if (firstKey != long.MaxValue && FindFirstTabGroupMember(container, (int)firstKey) is { } first)
                        return GetPreviousTabStop(null, first, true, ref didCycle);

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

            if (mode != KeyboardNavigationMode.None &&
                element != container &&
                EnterTabGroupMembers(element, container, mode, true, ref didCycle) is { } inside)
            {
                return inside;
            }

            if (mode == KeyboardNavigationMode.Contained)
                return null;

            if (element != container && IsTabStop(container))
                return container;

            if (!goDownOnly && GetTabParent(container) != null)
                return GetPreviousTabStop(container, null, false, ref didCycle);

            return null;
        }

        // Tries members in tab order: same TabIndex from the start element, then later values, then the Cycle wrap.
        private static IInputElement? EnterTabGroupMembers(
            IInputElement? element,
            IInputElement container,
            KeyboardNavigationMode mode,
            bool isReverse,
            ref bool didCycle)
        {
            if (element == null || element == container)
                return EnterMembersByTabOrderKey(container, long.MinValue, long.MaxValue, isReverse, ref didCycle);

            if (EnterSameIndexMembersFrom(element, container, isReverse, ref didCycle) is { } sameIndex)
                return sameIndex;

            var startKey = GetTabOrderKey(GetTabIndex(element), isReverse);

            if (EnterMembersByTabOrderKey(container, startKey, long.MaxValue, isReverse, ref didCycle) is { } later)
                return later;

            if (mode != KeyboardNavigationMode.Cycle)
                return null;

            if (GetTabParent(container) == null)
                didCycle = true;

            return EnterMembersByTabOrderKey(container, long.MinValue, startKey, isReverse, ref didCycle);
        }

        private static IInputElement? EnterSameIndexMembersFrom(
            IInputElement element,
            IInputElement container,
            bool isReverse,
            ref bool didCycle)
        {
            var tabIndex = GetTabIndex(element);
            var step = isReverse ? -1 : 1;

            if (!isReverse &&
                IsAvailableForTabbing(element) &&
                !IsTabScope(element, false) &&
                EnterChildMembers(element, tabIndex, false, ref didCycle) is { } descendant)
            {
                return descendant;
            }

            for (var current = element; current != container && GetTabParent(current) is { } parent; current = parent)
            {
                if (parent is Visual parentVisual && current is Visual currentVisual)
                {
                    var siblings = parentVisual.TypedVisualChildren;
                    var start = siblings.IndexOf(currentVisual);

                    for (var i = start + step; start >= 0 && i >= 0 && i < siblings.Count; i += step)
                    {
                        if (siblings[i] is IInputElement sibling &&
                            EnterMember(sibling, tabIndex, isReverse, ref didCycle) is { } tabStop)
                        {
                            return tabStop;
                        }
                    }
                }

                if (isReverse &&
                    parent != container &&
                    IsAvailableForTabbing(parent) &&
                    (IsTabScope(parent, true) || IsTabStop(parent)) &&
                    GetTabIndex(parent) == tabIndex &&
                    EnterPreviousMember(parent, ref didCycle) is { } enclosing)
                {
                    return enclosing;
                }
            }

            return null;
        }

        private static IInputElement? EnterMembersByTabOrderKey(
            IInputElement container,
            long fromKey,
            long toKey,
            bool isReverse,
            ref bool didCycle)
        {
            var lastKey = GetTabOrderKey(isReverse ? int.MinValue : int.MaxValue, isReverse);

            while (fromKey < lastKey)
            {
                var key = FindNextTabOrderKey(container, fromKey, toKey, isReverse, long.MaxValue);

                if (key == long.MaxValue)
                    return null;

                var tabIndex = (int)(isReverse ? -key : key);

                if (EnterChildMembers(container, tabIndex, isReverse, ref didCycle) is { } tabStop)
                    return tabStop;

                fromKey = key;
            }

            return null;
        }

        private static IInputElement? EnterChildMembers(
            IInputElement parent,
            int tabIndex,
            bool isReverse,
            ref bool didCycle)
        {
            if (parent is not Visual visual)
                return null;

            var children = visual.TypedVisualChildren;
            var step = isReverse ? -1 : 1;

            for (var i = isReverse ? children.Count - 1 : 0; i >= 0 && i < children.Count; i += step)
            {
                if (children[i] is IInputElement child &&
                    EnterMember(child, tabIndex, isReverse, ref didCycle) is { } tabStop)
                {
                    return tabStop;
                }
            }

            return null;
        }

        private static IInputElement? EnterMember(IInputElement element, int tabIndex, bool isReverse, ref bool didCycle)
        {
            if (!IsAvailableForTabbing(element))
                return null;

            var isScope = IsTabScope(element, isReverse);
            var isCandidate = (isScope || IsTabStop(element)) && GetTabIndex(element) == tabIndex;

            if (!isReverse && isCandidate && GetNextTabStop(null, element, true, ref didCycle) is { } next)
                return next;

            if (!isScope && EnterChildMembers(element, tabIndex, isReverse, ref didCycle) is { } descendant)
                return descendant;

            return isReverse && isCandidate ? EnterPreviousMember(element, ref didCycle) : null;
        }

        private static IInputElement? EnterPreviousMember(IInputElement element, ref bool didCycle) =>
            IsTabStop(element) && !IsTabGroup(element) ?
                element :
                GetPreviousTabStop(null, element, true, ref didCycle);

        private static long FindNextTabOrderKey(IInputElement parent, long fromKey, long toKey, bool isReverse, long best)
        {
            if (parent is not Visual visual)
                return best;

            var children = visual.TypedVisualChildren;

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] is not IInputElement child || !IsAvailableForTabbing(child))
                    continue;

                var isScope = IsTabScope(child, isReverse);

                if (isScope || IsTabStop(child))
                {
                    var key = GetTabOrderKey(GetTabIndex(child), isReverse);

                    if (key > fromKey && key <= toKey && key < best)
                        best = key;
                }

                if (!isScope)
                    best = FindNextTabOrderKey(child, fromKey, toKey, isReverse, best);
            }

            return best;
        }

        private static IInputElement? FindFirstTabGroupMember(IInputElement parent, int tabIndex)
        {
            if (parent is not Visual visual)
                return null;

            var children = visual.TypedVisualChildren;

            for (var i = 0; i < children.Count; i++)
            {
                if (children[i] is not IInputElement child || !IsAvailableForTabbing(child))
                    continue;

                var isScope = IsTabScope(child, false);

                if ((isScope || IsTabStop(child)) && GetTabIndex(child) == tabIndex)
                    return child;

                if (!isScope && FindFirstTabGroupMember(child, tabIndex) is { } member)
                    return member;
            }

            return null;
        }

        private static long GetTabOrderKey(int tabIndex, bool isReverse) => isReverse ? -(long)tabIndex : tabIndex;

        private static bool IsTabScope(IInputElement element, bool isReverse) =>
            IsTabGroup(element) || GetEntryOverride(element, isReverse) != null;

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

        private static bool IsTabStop(IInputElement element) =>
            element is InputElement { Focusable: true, IsTabStop: true } &&
            element.IsEffectivelyEnabled &&
            FocusHelpers.IsVisible(element);

        // Uses the element's own IsVisible so that a search can leave a hidden subtree.
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
