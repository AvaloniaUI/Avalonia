using System;
using System.Diagnostics;
using Avalonia.Interactivity;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Input
{
    public partial class FocusManager
    {
        private IFocusScope _owner;
        private IInputElement? _focusedElement;
        private FocusState _focusedElementState;
        private bool _isChangingFocus;
        private WeakReference<IInputElement>? _previousFocus;

        internal FocusManager(IFocusScope owner)
        {
            _owner = owner;
        }

        public IInputElement? FocusedElement => _focusedElement;

        public FocusState FocusedElementState => _focusedElementState;

        internal void SetFocusedElement(IInputElement? element, NavigationDirection direction = NavigationDirection.Next,
            FocusState state = FocusState.Programmatic, KeyModifiers keyModifiers = KeyModifiers.None, 
            bool allowCancelling = true, bool allowRedirecting = true)
        {
            if (_isChangingFocus)
            {
                throw new InvalidOperationException("Cannot change focus while focus is already changing." +
                    " Handle GettingFocus or LosingFocus events instead");
            }

            try
            {
                _isChangingFocus = true;
                Dispatcher.UIThread.VerifyAccess();

                Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                    .Log(nameof(FocusManager), "Focus change event starting for {Element}", element);

                Guid focusChangeID = Guid.NewGuid();
                var lastInputType = InputManager.Instance?.LastInputDeviceType ?? FocusInputDeviceKind.None;
                
                // First thing we need to do is check whether the incoming element is within the same scope as this
                // FocusManager. If it isn't _focusedElement should be null and we need to tell the other scope's
                // FocusManager to depart focus (raising losing/lose events)

                // Another window (most likely) holds the current focus, but we're switching to a different one
                // Clear the focus on that window before we continue here
                if (ActiveFocusRoot != null && ActiveFocusRoot != _owner)
                {
                    ActiveFocusRoot.FocusManager.ClearFocus();
                    Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                        .Log(nameof(FocusManager), "Focus root changed to {Root}", ActiveFocusRoot);
                }

                // If the element passed in is the same as the currently focused element, don't follow through with this
                // whole method. We still want to process it though, as we may be changing from Pointer to Keyboard focus
                // or vice versa. In this case, GettingFocus is not raised, but GotFocus is
                if (element == _focusedElement)
                {
                    if (_focusedElementState != state)
                    {
                        _focusedElementState = state;
                        if (element is InputElement inputElement)
                        {
                            inputElement.FocusState = GetActualFocusState(lastInputType, state);
                        }

                        Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                            .Log(nameof(FocusManager), "Focus state changed on {Ctrl}");
                        RaiseGotFocusEvent(_focusedElement, focusChangeID);
                    }

                    return;
                }

                // Now that we know we're changing focus, start by raising LosingFocus event
                var losingArgs = new LosingFocusEventArgs(allowCancelling, allowRedirecting)
                {
                    CorrelationID = focusChangeID,
                    NewFocusedElement = element,
                    OldFocusedElement = _focusedElement,
                    Direction = direction,
                    FocusState = state,
                    InputDevice = lastInputType,
                    KeyModifiers = keyModifiers
                };

                var result = RaiseLosingFocusEvents(losingArgs);

                // If user cancelled or changed the NewFocusedElement to an item that isn't Focusable,
                // we stop processing here
                if (!result)
                {
                    return;
                }

                var gettingArgs = new GettingFocusEventArgs(allowCancelling, allowRedirecting)
                {
                    CorrelationID = focusChangeID,
                    NewFocusedElement = losingArgs.NewFocusedElement,
                    OldFocusedElement = _focusedElement,
                    Direction = direction,
                    FocusState = state,
                    InputDevice = lastInputType,
                    KeyModifiers = keyModifiers
                };

                result = RaiseGettingFocusEvents(gettingArgs);

                // Same as above, stop here if NewFocusedElement isn't focusable or cancelled
                if (!result)
                {
                    return;
                }

                // Make sure we set the FocusState on the old element to Unfocused
                if (_focusedElement is InputElement oldFocusIE)
                {
                    oldFocusIE.FocusState = FocusState.Unfocused;
                }

                // We've now notified of focus changing, let's commit the change
                _focusedElement = gettingArgs.NewFocusedElement;
                _focusedElementState = state;

                if (_focusedElement is InputElement ie)
                {
                    // If the FocusState passed in here is programmatic, it will always be treated as a Pointer related
                    // state, and no focus rect will be drawn. So we'll use the last input device type to check whether
                    // the actual FocusState the control is getting, thus InputElement.FocusState can never be set
                    // to FocusState.Programmatic
                    ie.FocusState = GetActualFocusState(lastInputType, state);

                    Logger.TryGet(LogEventLevel.Debug, "Focus")?
                        .Log(nameof(FocusManager),
                        "Focus changed to {Ctrl} with state {State}",
                        _focusedElement?.ToString(), ie.FocusState);
                }

                EnsureActiveFocusRoot(_owner);

                RaiseLostFocusEvent(gettingArgs.OldFocusedElement, focusChangeID);
                RaiseGotFocusEvent(_focusedElement, focusChangeID);


                if (_focusedElement != null &&
                    (!_focusedElement.IsAttachedToVisualTree ||
                    _owner != _focusedElement?.VisualRoot as IInputRoot))
                {
                    ClearChildrenFocusWithin((_owner as IInputElement)!, true);
                }

                SetIsFocusWithin(gettingArgs.OldFocusedElement, _focusedElement);

                KeyboardDevice.Instance?.SetFocusedElement(_focusedElement);

                // TODO_FOCUS: There's probably a UIA focus event or notification we need to raise here too

                Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                    .Log("FocusManager", "Focus successfully changed to {Element} in {Root}", _focusedElement, _owner);
            }
            finally
            {
                _isChangingFocus = false;
            }
        }

        /// <summary>
        /// Clears the focus on a specific TopLevel when activating a different TopLevel
        /// (switching Windows)
        /// </summary>
        internal void ClearFocus()
        {
            // Save the currently focused item in this FocusManager. If we're switching windows and we switch back
            // we want to restore the correct item. 
            if (_focusedElement != null)
            {
                _previousFocus = new WeakReference<IInputElement>(_focusedElement);
            }
            else
            {
                _previousFocus = null;
            }

            SetFocusedElement(null, 
                state: FocusState.Unfocused, 
                allowCancelling: false,
                allowRedirecting: false);
        }

        /// <summary>
        /// Attemps to restore focus when a focus scope is reactivated
        /// </summary>
        internal void TryRestoreFocus()
        {            
            if (_previousFocus?.TryGetTarget(out var target) == true)
            {
                SetFocusedElement(target, state: FocusState.Programmatic);
            }
            else
            {
                // We haven't focused anything in this root previously, or
                // the element was removed and no longer exists
                // So find the first focusable item in the root
                target = FindFirstFocusableElement((_owner as IInputElement)!);

                SetFocusedElement(target, allowCancelling: false);
            }
        }

        /// <summary>
        /// Called when a focus scope closes, like closing a window
        /// </summary>
        /// <param name="root">The root to remove</param>
        internal void RemoveFocusRoot(IFocusScope root)
        {
            // It seems PopupRoot calls HandleClosed twice, once when the underlying
            // HWND is closed, and again in PopupRoot.Dispose(), calling this twice
            if (!_focusRoots.Contains(root))
                return;

            // We still need to raise the focus events here even if the root is closing
            // because we need LostFocus to be called in order for the FocusAdorner
            // to be properly detached (if it's active)
            root.FocusManager.SetFocusedElement(null,
                    state: FocusState.Unfocused,
                    allowRedirecting: false,
                    allowCancelling: false);

            if (_focusRoots.Count == 1)
            {
                _focusRoots.RemoveAt(0);
            }
            else if (ActiveFocusRoot == root)
            {
                // The root that is closing is currently active, close it out
                // and then focus the previous root                
                _focusRoots.RemoveAt(_focusRoots.Count - 1);

                // Active root is now the previously focused root
                var oldRootFM = ActiveFocusRoot!.FocusManager;
                oldRootFM.TryRestoreFocus();               
            }
            else
            {
                // If we're removing a root that isn't active (ex, a secondary window is
                // programmatically closed) 

                _focusRoots.Remove(root);
            }
        }

        /// <summary>
        /// Handles ensuring the correct focus root is set for the FocusManager
        /// </summary>
        /// <param name="newRoot">The desired root</param>
        private void EnsureActiveFocusRoot(IFocusScope newRoot)
        {
            if (ActiveFocusRoot == newRoot)
                return;

            var index = _focusRoots.IndexOf(newRoot);
            if (index >= 0)
            {
                // The root has already been registered, move it to the end
                // to signify it's the active focus root
                _focusRoots.RemoveAt(index);
                _focusRoots.Add(newRoot);
            }
            else
            {
                // This is a new focus root, add it
                _focusRoots.Add(newRoot);
            }
        }

        private bool RaiseLosingFocusEvents(LosingFocusEventArgs args)
        {
            var originalNewItem = args.NewFocusedElement;

            args.OldFocusedElement?.RaiseEvent(args);

            LosingFocus?.Invoke(this, args);

            // Now check if event was cancelled or if focus was moved via TrySetNewFocusedElement
            // We also need to verify that if the focus was moved, the new target is actually
            // focusable, otherwise we'll either move the focus to something that is (like a 
            // child item, if exists) or we'll cancel the focus change altogether

            // Easy out, no change made and not cancelled
            if (!args.Cancel && args.NewFocusedElement == originalNewItem)
                return true;

            // User cancelled this focus change action, return here
            // We also treat setting to null as a cancel
            if (args.Cancel || args.NewFocusedElement == null)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                    .Log("FocusManager-LosingFocus", "Focus change cancelled by user.");
                return false;
            }

            // User changed the item to move focus to, let's verify its valid here
            if (!IsFocusable(args.NewFocusedElement))
            {
                // TODO_Focus
                var childFocusable = args.Direction switch
                {
                    NavigationDirection.Previous => FindLastFocusableElement(args.NewFocusedElement),
                    NavigationDirection.Next => FindFirstFocusableElement(args.NewFocusedElement),
                    _ => null
                };

                if (childFocusable == null)
                {
                    Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                        .Log("FocusManager-LosingFocus", 
                        "New focused element changed to unfocusable item and no focusable child element" +
                        "was found. Focus change cancelled");

                    return false;
                }

                args.NewFocusedElement = childFocusable;
            }

            return true;
        }

        private bool RaiseGettingFocusEvents(GettingFocusEventArgs args)
        {
            var originalNewItem = args.NewFocusedElement;

            args.NewFocusedElement?.RaiseEvent(args);

            GettingFocus?.Invoke(this, args);

            // Now check if event was cancelled or if focus was moved via TrySetNewFocusedElement
            // We also need to verify that if the focus was moved, the new target is actually
            // focusable, otherwise we'll either move the focus to something that is (like a 
            // child item, if exists) or we'll cancel the focus change altogether

            // Easy out, no change made and not cancelled
            if (!args.Cancel && args.NewFocusedElement == originalNewItem)
                return true;

            // User cancelled this focus change action, return here
            // We also treat setting to null as a cancel
            if (args.Cancel || args.NewFocusedElement == null)
            {
                Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                    .Log("FocusManager-GettingFocus", "Focus change cancelled by user.");
                return false;
            }

            // User changed the item to move focus to, let's verify its valid here
            if (!IsFocusable(args.NewFocusedElement))
            {
                var childFocusable = args.Direction switch
                {
                    NavigationDirection.Previous => FindLastFocusableElement(args.NewFocusedElement),
                    NavigationDirection.Next => FindFirstFocusableElement(args.NewFocusedElement),
                    _ => null
                };

                if (childFocusable == null)
                {
                    Logger.TryGet(LogEventLevel.Debug, LogArea.Focus)?
                        .Log("FocusManager-GettingFocus",
                        "New focused element changed to unfocusable item and no focusable child element" +
                        "was found. Focus change cancelled");

                    return false;
                }

                args.NewFocusedElement = childFocusable;
            }

            return true;
        }

        private void RaiseLostFocusEvent(IInputElement? element, Guid id)
        {
            if (element != null)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    element.RaiseEvent(new RoutedEventArgs()
                    {
                        Source = element,
                        RoutedEvent = InputElement.LostFocusEvent
                    });

                });
            };

            Dispatcher.UIThread.Post(() =>
            {
                LostFocus?.Invoke(this, new FocusManagerLostFocusEventArgs(id, element));
            });
        }

        private void RaiseGotFocusEvent(IInputElement? element, Guid id)
        {
            Dispatcher.UIThread.Post(() =>
            {
                if (element != null)
                {
                    element.RaiseEvent(new RoutedEventArgs()
                    {
                        Source = element,
                        RoutedEvent = InputElement.GotFocusEvent
                    });
                }
            });

            Dispatcher.UIThread.Post(() =>
            {
                GotFocus?.Invoke(this, new FocusManagerGotFocusEventArgs(id, element));
            });
        }

        private void ClearFocusWithinAncestors(IInputElement? element)
        {
            var el = element;

            while (el != null)
            {
                if (el is InputElement ie)
                {
                    ie.IsKeyboardFocusWithin = false;
                }

                el = (IInputElement?)el.VisualParent;
            }
        }

        private void ClearFocusWithin(IInputElement element, bool clearRoot)
        {
            foreach (var visual in element.VisualChildren)
            {
                if (visual is IInputElement el && el.IsKeyboardFocusWithin)
                {
                    ClearFocusWithin(el, true);
                    break;
                }
            }

            if (clearRoot)
            {
                if (element is InputElement ie)
                {
                    ie.IsKeyboardFocusWithin = false;
                }
            }
        }

        private void SetIsFocusWithin(IInputElement? oldElement, IInputElement? newElement)
        {
            if (newElement == null && oldElement != null)
            {
                ClearFocusWithinAncestors(oldElement);
                return;
            }

            IInputElement? branch = null;

            var el = newElement;

            while (el != null)
            {
                if (el.IsKeyboardFocusWithin)
                {
                    branch = el;
                    break;
                }

                el = el.VisualParent as IInputElement;
            }

            el = oldElement;

            if (el != null && branch != null)
            {
                ClearFocusWithin(branch, false);
            }

            el = newElement;

            while (el != null && el != branch)
            {
                if (el is InputElement ie)
                {
                    ie.IsKeyboardFocusWithin = true;
                }

                el = el.VisualParent as IInputElement;
            }
        }

        private void ClearChildrenFocusWithin(IInputElement element, bool clearRoot)
        {
            foreach (var visual in element.VisualChildren)
            {
                if (visual is IInputElement el && el.IsKeyboardFocusWithin)
                {
                    ClearChildrenFocusWithin(el, true);
                    break;
                }
            }

            if (clearRoot && element is InputElement ie)
            {
                ie.IsKeyboardFocusWithin = false;
            }
        }
    }
}
