// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Markup.Xaml;

namespace Avalonia.Diagnostics.Views
{
    public class EventsView : UserControl
    {
        private readonly ListBox _events;

        public EventsView()
        {
            InitializeComponent();
            _events = this.FindControl<ListBox>("events");
        }

        private void RecordedEvents_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            _events.ScrollIntoView(_events.Items.OfType<FiredEvent>().LastOrDefault());
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
