// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Diagnostics.Models;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class EventTreeNode : EventTreeNodeBase
    {
        private readonly RoutedEvent _event;
        private readonly EventsViewModel _parentViewModel;
        private bool _isRegistered;
        private FiredEvent _currentEvent;

        public EventTreeNode(EventOwnerTreeNode parent, RoutedEvent @event, EventsViewModel vm)
            : base(parent, @event.Name)
        {
            Contract.Requires<ArgumentNullException>(@event != null);
            Contract.Requires<ArgumentNullException>(vm != null);

            _event = @event;
            _parentViewModel = vm;
        }

        public override bool? IsEnabled
        {
            get => base.IsEnabled;
            set
            {
                if (base.IsEnabled != value)
                {
                    base.IsEnabled = value;
                    UpdateTracker();
                    if (Parent != null && _updateParent)
                    {
                        try
                        {
                            Parent._updateChildren = false;
                            Parent.UpdateChecked();
                        }
                        finally
                        {
                            Parent._updateChildren = true;
                        }
                    }
                }
            }
        }

        private void UpdateTracker()
        {
            if (IsEnabled.GetValueOrDefault() && !_isRegistered)
            {
                // FIXME: This leaks event handlers.
                _event.AddClassHandler(typeof(object), HandleEvent, (RoutingStrategies)7, handledEventsToo: true);
                _isRegistered = true;
            }
        }

        private void HandleEvent(object sender, RoutedEventArgs e)
        {
            if (!_isRegistered || IsEnabled == false)
                return;
            if (sender is IVisual v && DevTools.BelongsToDevTool(v))
                return;

            var s = sender;
            var handled = e.Handled;
            var route = e.Route;

            Action handler = delegate
            {
                if (_currentEvent == null || !_currentEvent.IsPartOfSameEventChain(e))
                {
                    _currentEvent = new FiredEvent(e, new EventChainLink(s, handled, route));

                    _parentViewModel.RecordedEvents.Add(_currentEvent);

                    while (_parentViewModel.RecordedEvents.Count > 100)
                        _parentViewModel.RecordedEvents.RemoveAt(0);
                }
                else
                {
                    _currentEvent.AddToChain(new EventChainLink(s, handled, route));
                }
            };

            if (!Dispatcher.UIThread.CheckAccess())
                Dispatcher.UIThread.Post(handler);
            else
                handler();
        }
    }
}
