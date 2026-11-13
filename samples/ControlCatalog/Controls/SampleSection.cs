using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Metadata;

namespace ControlCatalog.Controls
{
    /// <summary>
    /// Framing options for a <see cref="SampleSection"/> stage.
    /// </summary>
    public enum SampleStage
    {
        Padded,
        Flush,
        None
    }

    /// <summary>
    /// One example on a <see cref="SamplePage"/>: a card with a title (<see cref="HeaderedContentControl.Header"/>),
    /// a description, the live example as content, and an optional options panel that sits beside the example
    /// on wide layouts and below it on narrow ones.
    /// </summary>
    [PseudoClasses(pcHasOptions, pcHasCode, pcNarrow, pcStageFlush, pcStageNone, pcFixedStage)]
    public class SampleSection : HeaderedContentControl
    {
        private const string pcHasOptions = ":has-options";
        private const string pcHasCode = ":has-code";
        private const string pcNarrow = ":narrow";
        private const string pcStageFlush = ":stage-flush";
        private const string pcStageNone = ":stage-none";
        private const string pcFixedStage = ":fixed-stage";

        /// <summary>
        /// Below this width the options panel moves under the example instead of beside it.
        /// </summary>
        private const double NarrowThreshold = 620;

        public static readonly StyledProperty<string?> DescriptionProperty =
            AvaloniaProperty.Register<SampleSection, string?>(nameof(Description));

        public static readonly StyledProperty<object?> OptionsProperty =
            AvaloniaProperty.Register<SampleSection, object?>(nameof(Options));

        public static readonly StyledProperty<string?> CodeProperty =
            AvaloniaProperty.Register<SampleSection, string?>(nameof(Code));

        public static readonly StyledProperty<CodeLanguage> CodeLanguageProperty =
            AvaloniaProperty.Register<SampleSection, CodeLanguage>(nameof(CodeLanguage));

        public static readonly StyledProperty<SampleStage> StageProperty =
            AvaloniaProperty.Register<SampleSection, SampleStage>(nameof(Stage), SampleStage.Padded);

        public static readonly StyledProperty<double> StageMinHeightProperty =
            AvaloniaProperty.Register<SampleSection, double>(nameof(StageMinHeight), 72);

        public static readonly StyledProperty<double> StageHeightProperty =
            AvaloniaProperty.Register<SampleSection, double>(nameof(StageHeight), double.NaN);

        /// <summary>
        /// What this example shows, in one sentence.
        /// </summary>
        public string? Description
        {
            get => GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        /// <summary>
        /// Controls that configure the example. Leave unset when the example has nothing to configure.
        /// </summary>
        public object? Options
        {
            get => GetValue(OptionsProperty);
            set => SetValue(OptionsProperty, value);
        }

        /// <summary>
        /// A short snippet shown under the example, collapsed until the reader opens it. Keep it to the
        /// lines that matter rather than reproducing the whole sample.
        /// </summary>
        public string? Code
        {
            get => GetValue(CodeProperty);
            set => SetValue(CodeProperty, value);
        }

        /// <summary>
        /// The language <see cref="Code"/> is highlighted as. Defaults to XAML.
        /// </summary>
        public CodeLanguage CodeLanguage
        {
            get => GetValue(CodeLanguageProperty);
            set => SetValue(CodeLanguageProperty, value);
        }

        /// <summary>
        /// How the example is framed. <see cref="SampleStage.Padded"/> is the default card stage; use
        /// <see cref="SampleStage.Flush"/> for content that paints its own edges and <see cref="SampleStage.None"/>
        /// for content that must sit directly on the card.
        /// </summary>
        public SampleStage Stage
        {
            get => GetValue(StageProperty);
            set => SetValue(StageProperty, value);
        }

        /// <summary>
        /// Minimum height of the stage, for examples that need room to be understood.
        /// </summary>
        public double StageMinHeight
        {
            get => GetValue(StageMinHeightProperty);
            set => SetValue(StageMinHeightProperty, value);
        }

        /// <summary>
        /// Fixed stage height, for content with no natural height: a demo app, a carousel, a GL surface.
        /// </summary>
        public double StageHeight
        {
            get => GetValue(StageHeightProperty);
            set => SetValue(StageHeightProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == OptionsProperty)
            {
                PseudoClasses.Set(pcHasOptions, change.NewValue is not null);
            }
            else if (change.Property == CodeProperty)
            {
                PseudoClasses.Set(pcHasCode, !string.IsNullOrWhiteSpace(change.NewValue as string));
            }
            else if (change.Property == StageProperty)
            {
                var stage = change.GetNewValue<SampleStage>();
                PseudoClasses.Set(pcStageFlush, stage == SampleStage.Flush);
                PseudoClasses.Set(pcStageNone, stage == SampleStage.None);
            }
            else if (change.Property == StageHeightProperty)
            {
                PseudoClasses.Set(pcFixedStage, !double.IsNaN(change.GetNewValue<double>()));
            }
        }

        protected override void OnSizeChanged(SizeChangedEventArgs e)
        {
            base.OnSizeChanged(e);

            if (e.WidthChanged)
            {
                PseudoClasses.Set(pcNarrow, e.NewSize.Width < NarrowThreshold);
            }
        }
    }
}
