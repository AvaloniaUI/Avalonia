using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    /// <summary>
    /// A form built the way a real data-entry form is: one flat list of heterogeneous rows —
    /// section headlines interleaved with fields of many different kinds — virtualized by a single
    /// <c>VirtualizingStackPanel</c>. Modelled on the VisibleFieldsControl of a production form
    /// filler, which is where the panel's hardest requirements come from:
    ///
    ///   * rows differ in height by more than an order of magnitude (a one-line number field next
    ///     to a 300px image or a long markdown block),
    ///   * a row's height changes *after* it has been realized, when async content arrives,
    ///   * the row kinds are not evenly distributed through the list,
    ///   * and the whole thing has to keep the scroll position steady while all of that happens.
    /// </summary>
    public class ListBoxComplexLayoutPageViewModel : ViewModelBase
    {
        private int _downloadDelayMs = 1200;
        private double _cacheLength = 0.5;
        private bool _recycleContent = true;
        private string _realizationStats = "";

        public ListBoxComplexLayoutPageViewModel()
        {
            Fields = new ObservableCollection<object>(BuildForm(sectionCount: 120));

            // Only the rows that have actually materialized a picture are re-fetched. Doing it to
            // the whole list would decode hundreds of bitmaps nobody has looked at.
            ReDownloadLoadedImagesCommand = MiniCommand.Create(() =>
            {
                foreach (var image in Fields.OfType<ImageFieldItem>())
                {
                    if (image.HasValue || image.IsDownloading)
                        image.ReDownload();
                }
            });

            AddSectionCommand = MiniCommand.Create(() =>
            {
                var number = Fields.OfType<HeadlineItem>().Count() + 1;
                foreach (var row in BuildSection(number, new Random(number)))
                    Fields.Add(row);
            });

            RemoveLastSectionCommand = MiniCommand.Create(() =>
            {
                var lastHeadline = -1;
                for (var i = Fields.Count - 1; i >= 0; i--)
                {
                    if (Fields[i] is HeadlineItem)
                    {
                        lastHeadline = i;
                        break;
                    }
                }

                if (lastHeadline < 0)
                    return;

                while (Fields.Count > lastHeadline)
                    Fields.RemoveAt(Fields.Count - 1);
            });
        }

        public ObservableCollection<object> Fields { get; }

        public MiniCommand ReDownloadLoadedImagesCommand { get; }
        public MiniCommand AddSectionCommand { get; }
        public MiniCommand RemoveLastSectionCommand { get; }

        /// <summary>How long an image field pretends to spend downloading.</summary>
        public int DownloadDelayMs
        {
            get => _downloadDelayMs;
            set
            {
                if (RaiseAndSetIfChanged(ref _downloadDelayMs, value))
                    ImageFieldItem.DownloadDelayMs = value;
            }
        }

        /// <summary>
        /// How much beyond the viewport the panel realizes, as a multiple of the viewport. Raising
        /// it gives an image row more time to finish loading before it is looked at, at the cost of
        /// keeping more containers alive; dropping it to 0 makes rows settle in plain view.
        /// </summary>
        public double CacheLength
        {
            get => _cacheLength;
            set => RaiseAndSetIfChanged(ref _cacheLength, value);
        }

        /// <summary>
        /// Whether a recycled container keeps the control tree it already has. Turning it off makes
        /// the row templates behave like plain <c>IDataTemplate</c>s: the container is still pooled,
        /// but its whole control tree is rebuilt every time it is reused for another row.
        /// </summary>
        public bool RecycleContent
        {
            get => _recycleContent;
            set => RaiseAndSetIfChanged(ref _recycleContent, value);
        }

        /// <summary>What the panel currently has realized — filled in by the page.</summary>
        public string RealizationStats
        {
            get => _realizationStats;
            set => RaiseAndSetIfChanged(ref _realizationStats, value);
        }


        private static IEnumerable<object> BuildForm(int sectionCount)
        {
            for (var section = 1; section <= sectionCount; section++)
            {
                foreach (var row in BuildSection(section, new Random(section)))
                    yield return row;
            }
        }

        /// <summary>
        /// One section: a headline followed by a handful of fields. The row kinds are chosen so the
        /// list is deliberately *not* uniform in either height or kind distribution — image and
        /// markdown rows only appear in some sections, which is what defeats guessing the set of
        /// row kinds from the first N rows.
        /// </summary>
        private static IEnumerable<object> BuildSection(int number, Random random)
        {
            var titles = new[] { "Observation", "Defect", "Assessment", "Measurements", "Sign-off" };
            var title = titles[(number - 1) % titles.Length];

            yield return new HeadlineItem
            {
                Numbering = number.ToString(),
                Title = title,
                UnsatisfiedFields = random.Next(0, 4),
                NegativeFields = random.Next(0, 3),
                IsRepeatable = title is "Observation" or "Defect",
            };

            var rows = new List<FieldItem>();

            rows.Add(new TextFieldItem
            {
                Title = "Description",
                FieldName = $"{title.ToLowerInvariant()}.description",
                IsMandatory = true,
                Value = string.Join(" ", Enumerable
                    .Range(0, random.Next(1, 9))
                    .Select(_ => Sentences[random.Next(Sentences.Length)])),
            });

            rows.Add(new NumberFieldItem
            {
                Title = "Quantity",
                FieldName = $"{title.ToLowerInvariant()}.quantity",
                Value = random.Next(1, 400),
                Unit = random.Next(2) == 0 ? "m²" : "pcs",
            });

            // Every third section carries a markdown block, so the row kind first appears well
            // past the head of the list.
            if (number % 3 == 0)
            {
                rows.Add(new MarkdownFieldItem
                {
                    Title = "Instructions",
                    FieldName = $"{title.ToLowerInvariant()}.instructions",
                    Markdown = MarkdownSamples[random.Next(MarkdownSamples.Length)],
                });
            }

            rows.Add(new ChoiceFieldItem
            {
                Title = "Severity",
                FieldName = $"{title.ToLowerInvariant()}.severity",
                IsMandatory = true,
                Options = new[] { "Low", "Medium", "High" },
                SelectedOption = random.Next(3) switch { 0 => "Low", 1 => "Medium", _ => "High" },
            });

            if (random.Next(2) == 0)
            {
                rows.Add(new ChecklistFieldItem
                {
                    Title = "Required actions",
                    FieldName = $"{title.ToLowerInvariant()}.actions",
                    Items = new ObservableCollection<ChecklistOption>(Actions
                        .OrderBy(_ => random.Next())
                        .Take(random.Next(2, Actions.Length + 1))
                        .Select(a => new ChecklistOption { Text = a, IsChecked = random.Next(2) == 0 })),
                });
            }

            // Every other section carries an image. This is the row whose height changes *after*
            // it is realized: it starts as a small placeholder and grows once the "download"
            // completes.
            if (number % 2 == 0)
            {
                rows.Add(new ImageFieldItem
                {
                    Title = "Photo",
                    FieldName = $"{title.ToLowerInvariant()}.photo",
                    AssetPath = SampleImages[random.Next(SampleImages.Length)],
                    LoadedHeight = 180 + random.Next(0, 5) * 40,
                });
            }

            rows.Add(new DateTimeFieldItem
            {
                Title = "Recorded",
                FieldName = $"{title.ToLowerInvariant()}.recorded",
                Timestamp = new DateTime(2024, 1, 1).AddDays(number).AddMinutes(random.Next(0, 1440)),
            });

            for (var i = 0; i < rows.Count; i++)
            {
                rows[i].SectionTitle = title;
                rows[i].IsLastFieldInSection = i == rows.Count - 1;
                rows[i].LastModified = $"Modified {new DateTime(2024, 1, 1).AddDays(number):yyyy-MM-dd} by {Users[random.Next(Users.Length)]}";
                rows[i].HasDescription = random.Next(3) == 0;
                yield return rows[i];
            }
        }

        private static readonly string[] Users = { "J. Doe", "A. Marek", "S. Fischer", "M. Weber" };

        private static readonly string[] Actions =
        {
            "Immediate repair", "Follow-up inspection", "Documentation only",
            "Notify site manager", "Isolate the area",
        };

        private static readonly string[] Sentences =
        {
            "Cracked concrete along the north wall, roughly two metres above floor level.",
            "Surface corrosion visible on the exposed bracket.",
            "Water ingress has stained the ceiling tiles in the adjacent room.",
            "The seal has perished and no longer sits flush against the frame.",
            "Measured deflection is within tolerance but trending upward since the last visit.",
            "No defect found; recorded for completeness.",
            "Access was restricted at the time of inspection, so this is a partial assessment.",
            "Previous repair appears sound with no sign of movement.",
        };

        private static readonly string[] MarkdownSamples =
        {
            """
            ## Important notes

            Please make sure that:

            - All **required** fields are filled in completely
            - Asset tags are scanned *where available*
            - Photos are clear and well lit

            Contact the inspection coordinator with any questions. Use `FORM-1042` as the reference.
            """,

            """
            ### Measurement procedure

            1. Zero the gauge before each reading
            2. Take **three** readings and record the *median*
            3. Note the ambient temperature

            A reading outside `±0.5 mm` must be flagged as a defect and photographed.
            """,

            """
            ## Safety

            **Do not** enter the void without a second person present.

            - Confirm isolation before opening any panel
            - Wear eye protection at all times
            - Report near misses the same day, however minor they seem

            This section exists mainly to be *tall*: a markdown row is several times the height of a
            number field, which is exactly the kind of variance the virtualizing panel has to price
            into its extent estimate without the scroll position drifting.
            """,
        };

        private static readonly string[] SampleImages =
        {
            "avares://ControlCatalog/Assets/delicate-arch-896885_640.jpg",
            "avares://ControlCatalog/Assets/hirsch-899118_640.jpg",
            "avares://ControlCatalog/Assets/maple-leaf-888807_640.jpg",
            "avares://ControlCatalog/Assets/image1.jpg",
            "avares://ControlCatalog/Assets/image2.jpg",
            "avares://ControlCatalog/Assets/image3.jpg",
            "avares://ControlCatalog/Assets/image4.jpg",
            "avares://ControlCatalog/Assets/image5.jpg",
        };
    }

    /// <summary>A section headline. Not a field — it is a row in the same flat list.</summary>
    public class HeadlineItem : ViewModelBase
    {
        private bool _isExpanded = true;

        public string Numbering { get; set; } = "";
        public string Title { get; set; } = "";
        public int UnsatisfiedFields { get; set; }
        public int NegativeFields { get; set; }
        public bool IsRepeatable { get; set; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set => RaiseAndSetIfChanged(ref _isExpanded, value);
        }

        public bool HasUnsatisfiedFields => UnsatisfiedFields > 0;
        public bool HasNegativeFields => NegativeFields > 0;
    }

    /// <summary>
    /// What every field row has in common — the chrome around the value: a mandatory bar, a title
    /// that may wrap to several lines, an optional description button and an audit footer.
    /// </summary>
    public abstract class FieldItem : ViewModelBase
    {
        public string Title { get; set; } = "";
        public string FieldName { get; set; } = "";
        public string SectionTitle { get; set; } = "";
        public bool IsMandatory { get; set; }
        public bool HasDescription { get; set; }
        public bool IsLastFieldInSection { get; set; }
        public string LastModified { get; set; } = "";

        public abstract bool HasValue { get; }

        /// <summary>Mandatory-but-empty is the state a form has to make obvious.</summary>
        public bool IsUnsatisfied => IsMandatory && !HasValue;

        /// <summary>
        /// The marker appended to the title of a mandatory field. A property rather than a second
        /// control so it flows with the title text and wraps with it.
        /// </summary>
        public string MandatoryMarker => IsMandatory ? " *" : "";
    }

    public class TextFieldItem : FieldItem
    {
        private string _value = "";

        public string Value
        {
            get => _value;
            set
            {
                if (RaiseAndSetIfChanged(ref _value, value))
                {
                    RaisePropertyChanged(nameof(HasValue));
                    RaisePropertyChanged(nameof(IsUnsatisfied));
                }
            }
        }

        public override bool HasValue => !string.IsNullOrWhiteSpace(Value);
    }

    public class NumberFieldItem : FieldItem
    {
        private double? _value;

        public double? Value
        {
            get => _value;
            set
            {
                if (RaiseAndSetIfChanged(ref _value, value))
                {
                    RaisePropertyChanged(nameof(HasValue));
                    RaisePropertyChanged(nameof(IsUnsatisfied));
                }
            }
        }

        public string Unit { get; set; } = "";

        public override bool HasValue => Value.HasValue;
    }

    public class DateTimeFieldItem : FieldItem
    {
        public DateTime Timestamp { get; set; }

        public string DateText => Timestamp.ToString("yyyy-MM-dd");
        public string TimeText => Timestamp.ToString("HH:mm");

        public override bool HasValue => true;
    }

    /// <summary>A short set of mutually exclusive options, rendered as a row of buttons.</summary>
    public class ChoiceFieldItem : FieldItem
    {
        private string? _selectedOption;

        public IReadOnlyList<string> Options { get; set; } = Array.Empty<string>();

        public string? SelectedOption
        {
            get => _selectedOption;
            set
            {
                if (RaiseAndSetIfChanged(ref _selectedOption, value))
                {
                    RaisePropertyChanged(nameof(HasValue));
                    RaisePropertyChanged(nameof(IsUnsatisfied));
                }
            }
        }

        public override bool HasValue => !string.IsNullOrEmpty(SelectedOption);
    }

    /// <summary>
    /// A checklist. Its height depends on how many options it has, so two rows of the same *kind*
    /// still differ in height — the panel cannot assume one size per row kind.
    /// </summary>
    public class ChecklistFieldItem : FieldItem
    {
        public ObservableCollection<ChecklistOption> Items { get; set; } = new();

        public override bool HasValue => Items.Any(i => i.IsChecked);
    }

    public class ChecklistOption : ViewModelBase
    {
        private bool _isChecked;

        public string Text { get; set; } = "";

        public bool IsChecked
        {
            get => _isChecked;
            set => RaiseAndSetIfChanged(ref _isChecked, value);
        }
    }

    /// <summary>Markdown rendered into TextBlock inlines. Tall, and its height depends on the text.</summary>
    public class MarkdownFieldItem : FieldItem
    {
        public string Markdown { get; set; } = "";

        public override bool HasValue => true;
    }

    /// <summary>
    /// An image field that "downloads" its picture. This is the interesting row for the
    /// virtualizing panel: it is realized at <see cref="PlaceholderHeight"/>, and some time later —
    /// while it may well be off screen, or while the user is scrolling past it — the picture
    /// arrives and the row jumps to <see cref="LoadedHeight"/>. Everything below it moves, and the
    /// panel has to absorb that without dragging the scroll position with it.
    /// </summary>
    public class ImageFieldItem : FieldItem
    {
        /// <summary>Shared so the page's delay slider affects rows that have not started yet.</summary>
        public static int DownloadDelayMs = 1200;

        private Bitmap? _image;
        private bool _isDownloading;
        private CancellationTokenSource? _cts;

        public ImageFieldItem()
        {
            ReDownloadCommand = MiniCommand.Create(ReDownload);
        }

        /// <summary>Drops this row's picture and fetches it again, so its height changes on demand.</summary>
        public MiniCommand ReDownloadCommand { get; }

        public string AssetPath { get; set; } = "";

        /// <summary>Height of the "not downloaded yet" placeholder.</summary>
        public double PlaceholderHeight => 84;

        /// <summary>Height once the picture is in. Varies per row on purpose.</summary>
        public double LoadedHeight { get; set; } = 260;

        public Bitmap? Image
        {
            get => _image;
            private set
            {
                if (RaiseAndSetIfChanged(ref _image, value))
                    RaisePropertyChanged(nameof(HasValue));
            }
        }

        public bool IsDownloading
        {
            get => _isDownloading;
            private set => RaiseAndSetIfChanged(ref _isDownloading, value);
        }

        public override bool HasValue => Image is not null;

        /// <summary>
        /// Called when the row is realized, i.e. when it first comes near the viewport — the same
        /// moment a real app would start fetching. Idempotent: scrolling a row in and out again
        /// must not restart a download or re-grow a row that is already loaded.
        /// </summary>
        public void StartDownloadIfNeeded()
        {
            if (Image is not null || IsDownloading)
                return;

            _cts = new CancellationTokenSource();
            _ = DownloadAsync(_cts.Token);
        }

        /// <summary>Drops the picture, cancelling any download in flight.</summary>
        public void Reset()
        {
            _cts?.Cancel();
            _cts = null;
            IsDownloading = false;
            Image = null;
        }

        /// <summary>Drops the picture and fetches it again — the row shrinks, then grows.</summary>
        public void ReDownload()
        {
            Reset();
            StartDownloadIfNeeded();
        }

        private async Task DownloadAsync(CancellationToken cancellationToken)
        {
            IsDownloading = true;

            try
            {
                await Task.Delay(DownloadDelayMs, cancellationToken).ConfigureAwait(false);

                var bitmap = await Task.Run(() =>
                {
                    using var stream = AssetLoader.Open(new Uri(AssetPath));
                    return new Bitmap(stream);
                }, cancellationToken).ConfigureAwait(false);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        bitmap.Dispose();
                        return;
                    }

                    Image = bitmap;
                    IsDownloading = false;
                });
            }
            catch (OperationCanceledException)
            {
                await Dispatcher.UIThread.InvokeAsync(() => IsDownloading = false);
            }
            catch
            {
                // Asset missing or undecodable — leave the placeholder in place.
                await Dispatcher.UIThread.InvokeAsync(() => IsDownloading = false);
            }
        }
    }
}
