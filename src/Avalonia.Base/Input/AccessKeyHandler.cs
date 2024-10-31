using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Input
{
    /// <summary>
    /// Handles access keys for a window.
    /// </summary>
    internal class AccessKeyHandler : IAccessKeyHandler
    {
        /// <summary>
        /// Defines the AccessKey attached event.
        /// </summary>
        public static readonly RoutedEvent<AccessKeyEventArgs> AccessKeyEvent =
            RoutedEvent.Register<AccessKeyEventArgs>(
                "AccessKey",
                RoutingStrategies.Bubble,
                typeof(AccessKeyHandler));

        /// <summary>
        /// Defines the AccessKeyPressed attached event.
        /// </summary>
        public static readonly RoutedEvent<AccessKeyPressedEventArgs> AccessKeyPressedEvent =
            RoutedEvent.Register<AccessKeyPressedEventArgs>(
                "AccessKeyPressed",
                RoutingStrategies.Bubble,
                typeof(AccessKeyHandler));

        /// <summary>
        /// The registered access keys.
        /// </summary>
        private readonly List<AccessKeyRegistration> _registrations = [];

        protected IReadOnlyList<AccessKeyRegistration> Registrations => _registrations;

        /// <summary>
        /// The window to which the handler belongs.
        /// </summary>
        private IInputRoot? _owner;

        /// <summary>
        /// Whether access keys are currently being shown;
        /// </summary>
        private bool _showingAccessKeys;

        /// <summary>
        /// Whether to ignore the Alt KeyUp event.
        /// </summary>
        private bool _ignoreAltUp;

        /// <summary>
        /// Whether the AltKey is down.
        /// </summary>
        private bool _altIsDown;

        /// <summary>
        /// Element to restore following AltKey taking focus.
        /// </summary>
        private WeakReference<IInputElement>? _restoreFocusElement;

        /// <summary>
        /// The window's main menu.
        /// </summary>
        private IMainMenu? _mainMenu;

        /// <summary>
        /// Gets or sets the window's main menu.
        /// </summary>
        public IMainMenu? MainMenu
        {
            get => _mainMenu;
            set
            {
                if (_mainMenu != null)
                {
                    _mainMenu.Closed -= MainMenuClosed;
                }

                _mainMenu = value;

                if (_mainMenu != null)
                {
                    _mainMenu.Closed += MainMenuClosed;
                }
            }
        }

        /// <summary>
        /// Sets the owner of the access key handler.
        /// </summary>
        /// <param name="owner">The owner.</param>
        /// <remarks>
        /// This method can only be called once, typically by the owner itself on creation.
        /// </remarks>
        public void SetOwner(IInputRoot owner)
        {
            if (_owner != null)
            {
                throw new InvalidOperationException("AccessKeyHandler owner has already been set.");
            }

            _owner = owner ?? throw new ArgumentNullException(nameof(owner));

            _owner.AddHandler(InputElement.KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            _owner.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Bubble);
            _owner.AddHandler(InputElement.KeyUpEvent, OnPreviewKeyUp, RoutingStrategies.Tunnel);
            _owner.AddHandler(InputElement.PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel);

            OnSetOwner(owner);
        }

        protected virtual void OnSetOwner(IInputRoot owner)
        {
        }

        /// <summary>
        /// Registers an input element to be associated with an access key.
        /// </summary>
        /// <param name="accessKey">The access key.</param>
        /// <param name="element">The input element.</param>
        public void Register(char accessKey, IInputElement element)
        {
            var key = NormalizeKey(accessKey.ToString());
            
            // remove dead elements with matching key
            for (var i = _registrations.Count - 1; i >= 0; i--)
            {
                var registration = _registrations[i];
                if (registration.Key == key && registration.GetInputElement() == null)
                {
                    _registrations.RemoveAt(i);    
                }
            }

            _registrations.Add(new AccessKeyRegistration(key, new WeakReference<IInputElement>(element)));
        }

        /// <summary>
        /// Unregisters the access keys associated with the input element.
        /// </summary>
        /// <param name="element">The input element.</param>
        public void Unregister(IInputElement element)
        {
            // remove element and all dead elements
            for (var i = _registrations.Count - 1; i >= 0; i--)
            {
                var registration = _registrations[i];
                var inputElement = registration.GetInputElement();
                if (inputElement == null || inputElement == element)
                {
                    _registrations.RemoveAt(i);    
                }
            }
        }

        /// <summary>
        /// Called when a key is pressed in the owner window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyDown(object? sender, KeyEventArgs e)
        {
            // if the owner (IInputRoot) does not have the keyboard focus, ignore all keyboard events
            // KeyboardDevice.IsKeyboardFocusWithin in case of a PopupRoot seems to only work once, so we created our own
            var isFocusWithinOwner = IsFocusWithinOwner(_owner!);
            if (!isFocusWithinOwner)
                return;

            if (e.Key is Key.LeftAlt or Key.RightAlt)
            {
                _altIsDown = true;

                if (MainMenu is not { IsOpen: true })
                {
                    var focusManager = FocusManager.GetFocusManager(e.Source as IInputElement);

                    // TODO: Use FocusScopes to store the current element and restore it when context menu is closed.
                    // Save currently focused input element.
                    var focusedElement = focusManager?.GetFocusedElement();
                    if (focusedElement is not null)
                        _restoreFocusElement = new WeakReference<IInputElement>(focusedElement);

                    // When Alt is pressed without a main menu, or with a closed main menu, show
                    // access key markers in the window (i.e. "_File").
                    _owner!.ShowAccessKeys = _showingAccessKeys = isFocusWithinOwner;
                }
                else
                {
                    // If the Alt key is pressed and the main menu is open, close the main menu.
                    CloseMenu();
                    _ignoreAltUp = true;

                    if (_restoreFocusElement?.TryGetTarget(out var restoreElement) ?? false)
                    {
                        Dispatcher.UIThread.Post(() => restoreElement.Focus());
                    }

                    _restoreFocusElement = null;
                }
            }
            else if (_altIsDown)
            {
                _ignoreAltUp = true;
            }
        }

        /// <summary>
        /// Called when a key is pressed in the owner window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnKeyDown(object? sender, KeyEventArgs e)
        {
            // if the owner (IInputRoot) does not have the keyboard focus, ignore all keyboard events
            // KeyboardDevice.IsKeyboardFocusWithin in case of a PopupRoot seems to only work once, so we created our own
            var isFocusWithinOwner = IsFocusWithinOwner(_owner!);
            if (!isFocusWithinOwner)
                return;

            if ((!e.KeyModifiers.HasAllFlags(KeyModifiers.Alt) || e.KeyModifiers.HasAllFlags(KeyModifiers.Control)) &&
                MainMenu?.IsOpen != true)
                return;

            e.Handled = ProcessKey(e.Key.ToString(), e.Source as IInputElement);
        }


        /// <summary>
        /// Handles the Alt/F10 keys being released in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewKeyUp(object? sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.LeftAlt:
                case Key.RightAlt:
                    _altIsDown = false;

                    if (_ignoreAltUp)
                    {
                        _ignoreAltUp = false;
                    }
                    else if (_showingAccessKeys && MainMenu != null)
                    {
                        MainMenu.Open();
                    }

                    break;
            }
        }

        /// <summary>
        /// Handles pointer presses in the window.
        /// </summary>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event args.</param>
        protected virtual void OnPreviewPointerPressed(object? sender, PointerEventArgs e)
        {
            if (_showingAccessKeys)
            {
                _owner!.ShowAccessKeys = false;
            }
        }

        /// <summary>
        /// Closes the <see cref="MainMenu"/> and performs other bookeeping.
        /// </summary>
        private void CloseMenu()
        {
            MainMenu!.Close();
            _owner!.ShowAccessKeys = _showingAccessKeys = false;
        }

        private void MainMenuClosed(object? sender, EventArgs e)
        {
            _owner!.ShowAccessKeys = false;
        }

        /// <summary>
        /// Processes the given key for the element's targets 
        /// </summary>
        /// <param name="key">The access key to process.</param>
        /// <param name="element">The element to get the targets which are in scope.</param>
        /// <returns>If there matches <c>true</c>, otherwise <c>false</c>.</returns>
        protected bool ProcessKey(string key, IInputElement? element)
        {
            key = NormalizeKey(key);
            var targets = SortByHierarchy(GetTargetsForSender(element, key));
            var result = ProcessKey(key, targets);
            return result != ProcessKeyResult.NoMatch;
        }

        private static string NormalizeKey(string key) => key.ToUpperInvariant();

        private static ProcessKeyResult ProcessKey(string key, List<IInputElement> targets)
        {
            if (!targets.Any())
                return ProcessKeyResult.NoMatch;

            var isSingleTarget = true;
            var lastWasFocused = false;

            IInputElement? effectiveTarget = null;

            var chosenIndex = 0;
            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                if (!IsTargetable(target))
                    continue;

                if (effectiveTarget == null)
                {
                    effectiveTarget = target;
                    chosenIndex = i;
                }
                else
                {
                    if (lastWasFocused)
                    {
                        effectiveTarget = target;
                        chosenIndex = i;
                    }

                    isSingleTarget = false;
                }

                lastWasFocused = target.IsFocused;
            }

            if (effectiveTarget == null)
                return ProcessKeyResult.NoMatch;

            var args = new AccessKeyEventArgs(key, isMultiple: !isSingleTarget);
            effectiveTarget.RaiseEvent(args);

            return chosenIndex == targets.Count - 1 ? ProcessKeyResult.LastMatch : ProcessKeyResult.MoreMatches;
        }

        /// <summary>
        /// Get the list of access key targets for the sender of the keyboard event.  If sender is null,
        /// pretend key was pressed in the active window.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private List<IInputElement> GetTargetsForSender(IInputElement? sender, string key)
        {
            // Find the scope for the sender -- will be matched against the possible targets' scopes
            var senderInfo = GetInfoForElement(sender, key);
            return GetTargetsForScope(key, sender, senderInfo);
        }

        private List<IInputElement> GetTargetsForScope(string key, IInputElement? sender,
            AccessKeyInformation senderInfo)
        {
            var possibleElements = CopyMatchingAndPurgeDead(key);

            if (!possibleElements.Any())
                return possibleElements;

            var finalTargets = new List<IInputElement>(1);

            // Go through all the possible elements, find the interesting candidates
            foreach (var element in possibleElements)
            {
                if (element != sender)
                {
                    if (!IsTargetable(element))
                        continue;

                    var elementInfo = GetInfoForElement(element, key);
                    if (elementInfo.Target == null)
                        continue;

                    finalTargets.Add(elementInfo.Target);
                }
                else
                {
                    // This is the same element that sent the event so it must be in the same scope.
                    // Just add it to the final targets
                    if (senderInfo.Target == null)
                        continue;

                    finalTargets.Add(senderInfo.Target);
                }
            }

            return finalTargets;
        }

        private static bool IsTargetable(IInputElement element) =>
            element is { IsEffectivelyEnabled: true, IsEffectivelyVisible: true };

        private List<IInputElement> CopyMatchingAndPurgeDead(string key)
        {
            var matches = new List<IInputElement>(_registrations.Count);
                
            // collect live elements with matching key and remove dead elements
            for (var i = _registrations.Count - 1; i >= 0; i--)
            {
                var registration = _registrations[i];
                var inputElement = registration.GetInputElement();
                if (inputElement != null)
                {
                    if (registration.Key == key)
                    {
                        matches.Add(inputElement);
                    }
                }
                else
                {
                    _registrations.RemoveAt(i);
                }
            }

            // since we collected the elements when iterating from back to front
            // we need to reverse them to ensure the original order
            matches.Reverse();
            
            return matches;
        }

        /// <summary>
        /// Returns targeting information for the given element.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="key"></param>
        /// <returns>AccessKeyInformation with target for the access key.</returns>
        private static AccessKeyInformation GetInfoForElement(IInputElement? element, string key)
        {
            var info = new AccessKeyInformation();
            if (element == null)
                return info;

            var args = new AccessKeyPressedEventArgs(key);
            element.RaiseEvent(args);
            info.Target = args.Target;

            return info;
        }

        /// <summary>
        /// Checks if the focused element is a descendent of the owner.
        /// </summary>
        /// <param name="owner">The owner to check.</param>
        /// <returns>If focused element is decendant of owner <c>true</c>, otherwise <c>false</c>. </returns>
        private static bool IsFocusWithinOwner(IInputRoot owner)
        {
            var focusedElement = KeyboardDevice.Instance?.FocusedElement;
            if (focusedElement is not InputElement inputElement)
                return false;

            var isAncestorOf = owner is Visual root && root.IsVisualAncestorOf(inputElement);
            return isAncestorOf;
        }

        /// <summary>
        /// Sorts the list of targets in hierarchical order to ensure that elements within a tab,
        /// for example, are processed before the next tab item in case of identical access keys
        /// </summary>
        private static List<IInputElement> SortByHierarchy(List<IInputElement> targets)
        {
            var sorted = new List<IInputElement>();
            var elements = targets.OfType<InputElement>().ToList();
            for (var i = 0; i < elements.Count; i++)
            {
                var parent = elements[i];
                if (sorted.Contains(parent))
                    continue;

                sorted.Add(parent);
                for (var j = i + 1; j < elements.Count; j++)
                {
                    var current = elements[j];
                    if (parent.IsLogicalAncestorOf(current))
                    {
                        sorted.Add(current);
                    }
                }
            }

            return sorted;
        }

        private enum ProcessKeyResult
        {
            NoMatch,
            MoreMatches,
            LastMatch
        }

        private struct AccessKeyInformation
        {
            public IInputElement? Target { get; set; }
        }
    }

    /// <summary>
    /// The inputs to an AccessKeyPressedEventHandler
    /// </summary>
    public class AccessKeyPressedEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// The constructor for AccessKeyPressed event args
        /// </summary>
        public AccessKeyPressedEventArgs()
        {
            RoutedEvent = AccessKeyHandler.AccessKeyPressedEvent;
            Key = null;
        }

        /// <summary>
        /// Constructor for AccessKeyPressed event args
        /// </summary>
        /// <param name="key"></param>
        public AccessKeyPressedEventArgs(string key) : this()
        {
            RoutedEvent = AccessKeyHandler.AccessKeyPressedEvent;
            Key = key;
        }

        /// <summary>
        /// Target element for the element that raised this event.
        /// </summary>
        /// <value></value>
        public IInputElement? Target { get; set; }

        /// <summary>
        /// Key that was pressed
        /// </summary>
        /// <value></value>
        public string? Key { get; }
    }

    /// <summary>
    /// Information pertaining to when the access key associated with an element is pressed
    /// </summary>
    public class AccessKeyEventArgs : RoutedEventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        internal AccessKeyEventArgs(string key, bool isMultiple)
        {
            RoutedEvent = AccessKeyHandler.AccessKeyEvent;

            Key = key;
            IsMultiple = isMultiple;
        }

        /// <summary>
        /// The key that was pressed which invoked this access key
        /// </summary>
        /// <value></value>
        public string Key { get; }

        /// <summary>
        /// Were there other elements which are also invoked by this key
        /// </summary>
        /// <value></value>
        public bool IsMultiple { get; }
    }

    internal class AccessKeyRegistration
    {
        private readonly WeakReference<IInputElement> _target;
        public string Key { get; }

        public AccessKeyRegistration(string key, WeakReference<IInputElement> target)
        {
            _target = target;
            Key = key;
        }

        public IInputElement? GetInputElement() =>
            _target.TryGetTarget(out var target) ? target : null;
    }
}
