using System;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Avalonia.Controls
{
    public partial class FocusManager : IFocusManager
    {
        private IInputRoot _owner;
        private IInputElement? _focusedElement;
        private FocusState _focusedElementState;
        private bool _isChangingFocus;

        internal FocusManager(IInputRoot owner)
        {
            _owner = owner;
        }

        public IInputElement? FocusedElement => _focusedElement;

        public FocusState FocusedElementState => _focusedElementState;

        internal void SetFocusedElement(IInputElement? element, NavigationDirection direction = NavigationDirection.Next,
            FocusState state = FocusState.Programmatic, bool allowCancelling = true)
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

                // TODO
            }
            finally
            {
                _isChangingFocus = false;
            }
        }

        void IFocusManager.SetFocusedElement(IInputElement? element, FocusState state) => 
            SetFocusedElement(element, state: state);

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
    }
}
