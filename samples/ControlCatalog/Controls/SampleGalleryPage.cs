using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

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

        private IReadOnlyList<SampleInfo> _samples = Array.Empty<SampleInfo>();
        private bool _opening;

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
        /// by <see cref="SampleGroups.Order"/>.
        /// </summary>
        public IReadOnlyList<SampleInfo> Samples
        {
            get => _samples;
            set => SetAndRaise(SamplesProperty, ref _samples, value ?? Array.Empty<SampleInfo>());
        }

        protected override Type StyleKeyOverride => typeof(SampleGalleryPage);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);
            RebuildContent();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SamplesProperty)
                RebuildContent();
        }

        private void RebuildContent() => Content = BuildHome();

        private Control BuildHome()
        {
            var stack = new StackPanel { Spacing = 20 };

            var description = new TextBlock
            {
                Classes = { "sample-description" },
                Text = Description,
                IsVisible = !string.IsNullOrEmpty(Description)
            };
            description.Bind(TextBlock.TextProperty, this.GetObservable(DescriptionProperty));
            stack.Children.Add(description);

            foreach (var (group, samples) in GroupSamples())
            {
                var groupStack = new StackPanel { Spacing = 10 };
                groupStack.Children.Add(new TextBlock
                {
                    Classes = { "sample-group" },
                    Text = group
                });

                var grid = new CardGrid { MinItemWidth = 220 };
                foreach (var sample in samples)
                {
                    grid.Children.Add(CreateCard(sample));
                }

                groupStack.Children.Add(grid);
                stack.Children.Add(groupStack);
            }

            return new ScrollViewer
            {
                Padding = new Thickness(24, 20, 24, 28),
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Content = stack
            };
        }

        private IEnumerable<(string Group, List<SampleInfo> Samples)> GroupSamples()
        {
            var groups = new List<(string Group, List<SampleInfo> Samples)>();

            foreach (var sample in Samples)
            {
                var index = groups.FindIndex(g => string.Equals(g.Group, sample.Group, StringComparison.Ordinal));
                if (index < 0)
                {
                    groups.Add((sample.Group, new List<SampleInfo>()));
                    index = groups.Count - 1;
                }

                groups[index].Samples.Add(sample);
            }

            // Known groups in canonical order, then unknown groups in the order they first appear.
            var ordered = new List<(string Group, List<SampleInfo> Samples)>(groups.Count);
            foreach (var index in SampleGroups.Order.Select(SampleGroups.IndexOf).OrderBy(i => i))
            {
                foreach (var group in groups)
                {
                    if (SampleGroups.IndexOf(group.Group) == index)
                    {
                        ordered.Add(group);
                    }
                }
            }

            foreach (var group in groups)
            {
                if (SampleGroups.IndexOf(group.Group) == int.MaxValue)
                {
                    ordered.Add(group);
                }
            }

            return ordered;
        }

        private static Control CreateErrorContent(SampleInfo sample, Exception ex)
        {
            var text = new SelectableTextBlock
            {
                Classes = { "sample-caption" },
                Text = ex.ToString(),
                FontFamily = new FontFamily("Cascadia Code,Cascadia Mono,Consolas,Menlo,monospace")
            };

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
                        text
                    }
                }
            };
        }

        private Button CreateCard(SampleInfo sample)
        {
            var card = new Button
            {
                Classes = { "sample-card" },
                Content = new StackPanel
                {
                    Spacing = 4,
                    Children =
                    {
                        new TextBlock
                        {
                            Classes = { "sample-card-title" },
                            Text = sample.Title
                        },
                        new TextBlock
                        {
                            Classes = { "sample-card-description" },
                            Text = sample.Description
                        }
                    }
                }
            };

            if (this.TryFindResource("SampleCardTheme", out var theme) && theme is ControlTheme cardTheme)
            {
                card.Theme = cardTheme;
            }

            AutomationProperties.SetName(card, sample.Title);
            AutomationProperties.SetHelpText(card, sample.Description);
            card.Click += async (_, _) => await OpenAsync(sample);
            return card;
        }

        /// <summary>
        /// Opens <paramref name="sample"/> on the hosting navigation stack, creating its content first. Public so
        /// the shell can deep-link to a sample from the navigation drawer as well as from a card.
        /// </summary>
        public async Task OpenAsync(SampleInfo sample)
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
                    // The shell paints the catalog page background, so a sample only draws its own content.
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
    }
}
