using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using MiniMvvm;

namespace ControlCatalog.Controls
{
    /// <summary>
    /// A catalog page for controls with many substantial samples. The page shows its <see cref="Description"/>
    /// and a grouped grid of cards built from <see cref="Samples"/>, and opens a sample on the hosting
    /// <see cref="NavigationPage"/>. Keeping the samples on the host stack leaves a single navigation bar on
    /// screen: the page title and the drawer toggle here, the sample title and the back button once a sample
    /// is open. Samples are constructed only when opened.
    /// </summary>
    public class SampleGalleryPage : ContentPage
    {
        public static readonly StyledProperty<string?> DescriptionProperty =
            AvaloniaProperty.Register<SampleGalleryPage, string?>(nameof(Description));

        public static readonly DirectProperty<SampleGalleryPage, IReadOnlyList<SampleInfo>> SamplesProperty =
            AvaloniaProperty.RegisterDirect<SampleGalleryPage, IReadOnlyList<SampleInfo>>(
                nameof(Samples), o => o.Samples, (o, v) => o.Samples = v);

        public static readonly DirectProperty<SampleGalleryPage, IReadOnlyList<SampleInfoGroup>> GroupsProperty =
            AvaloniaProperty.RegisterDirect<SampleGalleryPage, IReadOnlyList<SampleInfoGroup>>(
                nameof(Groups), o => o.Groups);

        private IReadOnlyList<SampleInfo> _samples = [];
        private IReadOnlyList<SampleInfoGroup> _groups = [];
        private bool _opening;

        public SampleGalleryPage()
        {
            OpenSampleCommand = MiniCommand.Create<SampleInfo>(sample => _ = OpenAsync(sample));
        }

        /// <summary>
        /// One or two sentences saying what the control is for. Shown above the sample cards.
        /// </summary>
        public string? Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// The samples to offer. Cards are grouped by <see cref="SampleInfo.Group"/> and the groups are ordered
        /// as in <see cref="SampleGroups"/>.
        /// </summary>
        public IReadOnlyList<SampleInfo> Samples
        {
            get => _samples;
            set => SetAndRaise(SamplesProperty, ref _samples, value ?? []);
        }

        public IReadOnlyList<SampleInfoGroup> Groups
        {
            get => _groups;
            private set => SetAndRaise(GroupsProperty, ref _groups, value);
        }

        public ICommand OpenSampleCommand { get; }

        // Pages derive from this class, and a theme is looked up by the exact type.
        protected override Type StyleKeyOverride => typeof(SampleGalleryPage);

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SamplesProperty)
            {
                // GroupBy keeps first appearance order, so unknown groups stay in registry order at the end.
                Groups = Samples
                    .GroupBy(sample => sample.Group)
                    .OrderBy(group => SampleGroups.IndexOf(group.Key))
                    .Select(group => new SampleInfoGroup(group.Key, group.ToArray()))
                    .ToArray();
            }
        }

        private async Task OpenAsync(SampleInfo sample)
        {
            var navigation = Navigation;
            if (_opening || navigation is null)
            {
                return;
            }

            _opening = true;
            try
            {
                Control content;
                try
                {
                    content = sample.Factory();
                }
                catch (Exception ex)
                {
                    content = CreateErrorContent(sample, ex);
                }

                var page = new ContentPage
                {
                    Header = sample.Title,
                    // Transparent like the catalog pages, so window transparency shows through.
                    Background = Brushes.Transparent,
                    Content = content,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch
                };
                await navigation.PushAsync(page, null);
            }
            finally
            {
                _opening = false;
            }
        }

        private static Control CreateErrorContent(SampleInfo sample, Exception ex)
        {
            return new ScrollViewer
            {
                Padding = new Thickness(24, 20),
                Content = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new TextBlock
                        {
                            Classes = { "sample-card-title" },
                            Text = "The sample \"" + sample.Title + "\" failed to load."
                        },
                        new SelectableTextBlock
                        {
                            Classes = { "sample-caption" },
                            Text = ex.ToString()
                        }
                    }
                }
            };
        }
    }
}
