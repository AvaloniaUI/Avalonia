using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Diagnostics.Models;
using Avalonia.Diagnostics.ViewModels;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

namespace Avalonia.Diagnostics.Views
{
    internal class EventsPageView : UserControl
    {
        private readonly ListBox _events;
        private IDisposable? _adorner;

        public EventsPageView()
        {
            InitializeComponent();
            _events = this.GetControl<ListBox>("EventsList");
        }

        public void NavigateTo(object sender, TappedEventArgs e)
        {
            if (DataContext is EventsPageViewModel vm && sender is Control control)
            {
                switch (control.Tag)
                {
                    case EventChainLink chainLink:
                    {
                        vm.RequestTreeNavigateTo(chainLink);
                        break;
                    }
                    case RoutedEvent evt:
                    {
                        vm.SelectEventByType(evt);

                        break;
                    }
                }
            }
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);

            if (DataContext is EventsPageViewModel vm)
            {
                vm.RecordedEvents.CollectionChanged += OnRecordedEventsChanged;
            }
        }

        private void OnRecordedEventsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (sender is ObservableCollection<FiredEvent> events)
            {
                var evt = events.LastOrDefault();

                if (evt is null)
                {
                    return;
                }

                Dispatcher.UIThread.Post(() => _events.ScrollIntoView(evt));
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ListBoxItem_PointerEntered(object? sender, PointerEventArgs e)
        {
            if (DataContext is EventsPageViewModel vm 
                && sender is ListBoxItem control 
                && control.DataContext is EventChainLink chainLink
                && chainLink.Handler is Visual visual)
            {
                _adorner = Controls.ControlHighlightAdorner.Add(visual, vm.MainView.ShouldVisualizeMarginPadding);
            }
        }

        private void ListBoxItem_PointerExited(object? sender, PointerEventArgs e)
        {
            _adorner?.Dispose();
        }
    }
}
