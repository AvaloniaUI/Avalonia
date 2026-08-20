using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public partial class ListBoxComplexLayoutPage : ContentPage
    {
        private readonly ListBoxComplexLayoutPageViewModel _viewModel;
        private readonly DispatcherTimer _statsTimer;

        public ListBoxComplexLayoutPage()
        {
            InitializeComponent();

            _viewModel = new ListBoxComplexLayoutPageViewModel();
            DataContext = _viewModel;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            _statsTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _statsTimer.Tick += (_, _) => UpdateRealizationStats();
        }

        /// <summary>The row templates, which are also where the build counter lives.</summary>
        private FieldTemplateSelector? Templates =>
            this.FindControl<ListBox>("FieldsListBox")?.ItemTemplate as FieldTemplateSelector;

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(ListBoxComplexLayoutPageViewModel.RecycleContent))
                return;

            if (Templates is not { } templates)
                return;

            templates.RecycleContent = _viewModel.RecycleContent;

            // The counter restarts so the two settings can be compared over the same scrolling.
            // Rows already realized keep the control tree they have; the new setting takes effect
            // as containers are recycled, i.e. as soon as you scroll. (Re-assigning ItemTemplate to
            // force them all to rebuild leaves the presenters holding an empty child — the rows go
            // blank and never recover.)
            templates.ResetBuilds();
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _statsTimer.Start();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _statsTimer.Stop();
        }

        /// <summary>
        /// The row this container shows has changed — either it was just built for an image row, or
        /// it was recycled from one image row onto another. Both are the moment a real app would
        /// start fetching. <see cref="ImageFieldItem.StartDownloadIfNeeded"/> is idempotent, so a
        /// row scrolled out and back in does not download twice.
        /// </summary>
        private void OnImageFieldDataContextChanged(object? sender, EventArgs e)
        {
            if (sender is Control { DataContext: ImageFieldItem item })
                item.StartDownloadIfNeeded();
        }

        /// <summary>
        /// Shows how little of the list is actually alive: the realized index range, how many
        /// containers exist for it, and the extent the panel is reporting for the whole list.
        /// </summary>
        private void UpdateRealizationStats()
        {
            var listBox = this.FindControl<ListBox>("FieldsListBox");
            var panel = listBox?.GetVisualDescendants().OfType<VirtualizingStackPanel>().FirstOrDefault();

            if (panel is null || panel.FirstRealizedIndex < 0)
            {
                _viewModel.RealizationStats = "Nothing realized yet.";
                return;
            }

            var containers = panel.Children.Count(c => c.IsVisible);
            var scroll = listBox!.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();

            var images = _viewModel.Fields.OfType<ImageFieldItem>().ToList();
            var loaded = images.Count(i => i.HasValue);
            var pending = images.Count(i => i.IsDownloading);

            _viewModel.RealizationStats =
                $"rows {panel.FirstRealizedIndex}..{panel.LastRealizedIndex} of {_viewModel.Fields.Count}   " +
                $"containers {containers}   " +
                $"row trees built {Templates?.Builds ?? 0}   " +
                (scroll is not null
                    ? $"offset {scroll.Offset.Y,8:0}  extent {scroll.Extent.Height,9:0}   "
                    : "") +
                $"images loaded {loaded}  downloading {pending}";
        }
    }
}
