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
        private RoutedEventArgs _eventArgs;

        private ChainLink _handledBy;
        private ChainLink _originator;

        public FiredEvent(RoutedEventArgs eventArgs, ChainLink originator)
        {
            Contract.Requires<ArgumentNullException>(eventArgs != null);
            Contract.Requires<ArgumentNullException>(originator != null);

            this._eventArgs = eventArgs;
            this._originator = originator;
            AddToChain(originator);
        }

        public bool IsPartOfSameEventChain(RoutedEventArgs e)
        {
            return e == _eventArgs;
        }

        public RoutedEvent Event => _eventArgs.RoutedEvent;

        public bool IsHandled => HandledBy?.Handled == true;

        public ObservableCollection<ChainLink> EventChain { get; } = new ObservableCollection<ChainLink>();

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

        public ChainLink Originator
        {
            get { return _originator; }
            set
            {
                if (_originator != value)
                {
                    _originator = value;
                    RaisePropertyChanged();
                    RaisePropertyChanged(nameof(DisplayText));
                }
            }
        }

        public ChainLink HandledBy
        {
            get { return _handledBy; }
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
            AddToChain(new ChainLink(handler, handled, route));
        }

        public void AddToChain(ChainLink link)
        {
            EventChain.Add(link);
            if (HandledBy == null && link.Handled)
                HandledBy = link;
        }
    }
}
