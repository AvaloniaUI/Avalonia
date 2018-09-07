// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Input;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class EventsViewModel : ViewModelBase
    {
        private IControl _root;
        private FiredEvent _selectedEvent;
        private ICommand ClearCommand { get; }

        public EventsViewModel(IControl root)
        {
            this._root = root;
            this.Nodes = RoutedEventRegistry.Instance.GetAllRegistered()
                .GroupBy(e => e.OwnerType)
                .OrderBy(e => e.Key.Name)
                .Select(g => new ControlTreeNode(g.Key, g, this))
                .ToArray();
        }

        private void ClearExecute()
        {
            Action action = delegate
            {
                RecordedEvents.Clear();
            };
            if (!Dispatcher.UIThread.CheckAccess())
            {
                Dispatcher.UIThread.Post(action);
            }
            else
            {
                action();
            }
        }

        public EventTreeNode[] Nodes { get; }

        public ObservableCollection<FiredEvent> RecordedEvents { get; } = new ObservableCollection<FiredEvent>();

        public FiredEvent SelectedEvent
        {
            get => _selectedEvent;
            set => RaiseAndSetIfChanged(ref _selectedEvent, value);
        }
    }

    internal class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (bool)value ? Brushes.LightGreen : Brushes.Transparent;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
