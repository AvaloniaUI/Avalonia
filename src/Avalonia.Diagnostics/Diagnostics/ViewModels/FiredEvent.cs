// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using Avalonia.Diagnostics.Models;
using Avalonia.Interactivity;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class FiredEvent : ViewModelBase
    {
        private readonly RoutedEventArgs _eventArgs;
        private EventChainLink _handledBy;

        public FiredEvent(RoutedEventArgs eventArgs, EventChainLink originator)
        {
            Contract.Requires<ArgumentNullException>(eventArgs != null);
            Contract.Requires<ArgumentNullException>(originator != null);

            _eventArgs = eventArgs;
            Originator = originator;
            AddToChain(originator);
        }

        public bool IsPartOfSameEventChain(RoutedEventArgs e)
        {
            return e == _eventArgs;
        }

        public RoutedEvent Event => _eventArgs.RoutedEvent;

        public bool IsHandled => HandledBy?.Handled == true;

        public ObservableCollection<EventChainLink> EventChain { get; } = new ObservableCollection<EventChainLink>();

        public string DisplayText
        {
            get
            {
                if (IsHandled)
                {
                    return $"{Event.Name} on {Originator.HandlerName};" + Environment.NewLine +
                           $"strategies: {Event.RoutingStrategies}; handled by: {HandledBy.HandlerName}";
                }

                return $"{Event.Name} on {Originator.HandlerName}; strategies: {Event.RoutingStrategies}";
            }
        }

        public EventChainLink Originator { get; }

        public EventChainLink HandledBy
        {
            get => _handledBy;
            set
            {
                if (_handledBy != value)
                {
                    _handledBy = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(IsHandled));
                    RaisePropertyChanged(nameof(DisplayText));
                }
            }
        }

        public void AddToChain(object handler, bool handled, RoutingStrategies route)
        {
            AddToChain(new EventChainLink(handler, handled, route));
        }

        public void AddToChain(EventChainLink link)
        {
            EventChain.Add(link);
            if (HandledBy == null && link.Handled)
                HandledBy = link;
        }
    }
}
