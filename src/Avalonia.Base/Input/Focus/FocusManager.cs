using System;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Input
{
    public partial class FocusManager
    {
        private IInputRoot _owner;
        private IInputElement? _focusedElement;
        private FocusState _focusedElementState;
        private bool _isChangingFocus;
        private RestoreFocusInfo _restoreFocusInfo;

        internal FocusManager(IInputRoot owner)
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

                Guid focusChangeID = Guid.NewGuid();
                var lastInputType = InputManager.Instance?.LastInputDeviceType ?? FocusInputDeviceKind.None;
                
                // First thing we need to do is check whether the incoming element is within the same scope as this
                // FocusManager. If it isn't _focusedElement should be null and we need to tell the other scope's
                // FocusManager to depart focus (raising losing/lose events)

                // Another window (most likely) holds the current focus, but we're switching to a different one
                // Clear the focus on that window before we continue here
                if (_activeFocusRoot != null && _activeFocusRoot != _owner)
                {
                    _previousFocusRoot = new WeakReference<IInputRoot>(_activeFocusRoot);
                    ClearFocus(_activeFocusRoot);
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
                }

                _activeFocusRoot = _owner;

                RaiseLostFocusEvent(gettingArgs.OldFocusedElement, focusChangeID);
                RaiseGotFocusEvent(_focusedElement, focusChangeID);

                // TODO_FOCUS: probably can merge this logic from KeyboardDevice into FocusManager
                KeyboardDevice.Instance?.SetFocusedElement(_focusedElement);

                // TODO_FOCUS: There's probably a UIA focus event or notification we need to raise here too
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
        internal void ClearFocus(IInputRoot root)
        {
            // Save the currently focused item in this FocusManager. If we're switching windows and we switch back
            // we want to restore the correct item. 
            _restoreFocusInfo = new RestoreFocusInfo
            {
                LastFocusedElement = _focusedElement != null ? new WeakReference<IInputElement?>(_focusedElement) : null,
                LastFocusState = _focusedElementState
            };

            root.FocusManager.SetFocusedElement(null, state: FocusState.Unfocused, allowCancelling: false);
        }

        /// <summary>
        /// Attemps to restore focus when a TopLevel is reactivated
        /// </summary>
        internal void TryRestoreFocus()
        {            
            if (_restoreFocusInfo.LastFocusedElement?.TryGetTarget(out var target) == true)
            {
                SetFocusedElement(target, state: _restoreFocusInfo.LastFocusState);
            }
            else
            {
                _activeFocusRoot = _owner;
                // We haven't focused anything in this root previously, or
                // the element was removed and no longer exists
                // So find the first focusable item in the root
                TryMoveFocus(NavigationDirection.Next);
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

        private struct RestoreFocusInfo
        {
            public WeakReference<IInputElement?>? LastFocusedElement;
            public FocusState LastFocusState;
        }
    }
}
