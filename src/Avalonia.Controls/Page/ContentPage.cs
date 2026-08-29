using System;
using Avalonia.Automation.Peers;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// A page that displays a single piece of content with optional top and bottom command bars.
    /// </summary>
    [TemplatePart("PART_ContentPresenter", typeof(ContentPresenter))]
    [TemplatePart("PART_TopCommandBar", typeof(ContentPresenter))]
    [TemplatePart("PART_BottomCommandBar", typeof(ContentPresenter))]
    public class ContentPage : Page
    {
        private ContentPresenter? _contentPresenter;
        private ContentPresenter? _topCommandBarPresenter;
        private ContentPresenter? _bottomCommandBarPresenter;

        /// <summary>
        /// Defines the <see cref="Content"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> ContentProperty =
            ContentControl.ContentProperty.AddOwner<ContentPage>(new StyledPropertyMetadata<object?>(
                coerce: (_, val) =>
                {
                    if (val is Page)
                        throw new InvalidOperationException(
                            "A Page cannot be used as the content of a ContentPage. Use a MultiPage subclass such as NavigationPage, TabbedPage, DrawerPage, or CarouselPage to host child pages.");
                    return val;
                }));

        /// <summary>
        /// Defines the <see cref="ContentTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> ContentTemplateProperty =
            ContentControl.ContentTemplateProperty.AddOwner<ContentPage>();

        /// <summary>
        /// Defines the <see cref="AutomaticallyApplySafeAreaPadding"/> property.
        /// </summary>
        public static readonly StyledProperty<bool> AutomaticallyApplySafeAreaPaddingProperty =
            AvaloniaProperty.Register<ContentPage, bool>(nameof(AutomaticallyApplySafeAreaPadding), defaultValue: true);

        /// <summary>
        /// Defines the <see cref="TopCommandBar"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> TopCommandBarProperty =
            AvaloniaProperty.Register<ContentPage, object?>(nameof(TopCommandBar));

        /// <summary>
        /// Defines the <see cref="BottomCommandBar"/> property.
        /// </summary>
        public static readonly StyledProperty<object?> BottomCommandBarProperty =
            AvaloniaProperty.Register<ContentPage, object?>(nameof(BottomCommandBar));

        /// <summary>
        /// Defines the <see cref="HorizontalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<HorizontalAlignment> HorizontalContentAlignmentProperty =
            ContentControl.HorizontalContentAlignmentProperty.AddOwner<ContentPage>();

        /// <summary>
        /// Defines the <see cref="VerticalContentAlignment"/> property.
        /// </summary>
        public static readonly StyledProperty<VerticalAlignment> VerticalContentAlignmentProperty =
            ContentControl.VerticalContentAlignmentProperty.AddOwner<ContentPage>();

        static ContentPage()
        {
            ContentProperty.Changed.AddClassHandler<ContentPage>((x, e) => x.ContentChanged(e));
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of the content within the page.
        /// </summary>
        public HorizontalAlignment HorizontalContentAlignment
        {
            get => GetValue(HorizontalContentAlignmentProperty);
            set => SetValue(HorizontalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment of the content within the page.
        /// </summary>
        public VerticalAlignment VerticalContentAlignment
        {
            get => GetValue(VerticalContentAlignmentProperty);
            set => SetValue(VerticalContentAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the page content.
        /// </summary>
        /// <remarks>
        /// The content must not be another <see cref="Page"/>. Use a <see cref="MultiPage"/>
        /// implementation such as <see cref="NavigationPage"/>, <see cref="TabbedPage"/>,
        /// <see cref="DrawerPage"/>, or <see cref="CarouselPage"/> to host child pages.
        /// </remarks>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the assigned value is a <see cref="Page"/>.
        /// </exception>
        [Content]
        [DependsOn(nameof(ContentTemplate))]
        public object? Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to display <see cref="Content"/>.
        /// </summary>
        public IDataTemplate? ContentTemplate
        {
            get => GetValue(ContentTemplateProperty);
            set => SetValue(ContentTemplateProperty, value);
        }

        /// <summary>
        /// Gets or sets whether safe-area padding is automatically applied to the content presenter.
        /// </summary>
        public bool AutomaticallyApplySafeAreaPadding
        {
            get => GetValue(AutomaticallyApplySafeAreaPaddingProperty);
            set => SetValue(AutomaticallyApplySafeAreaPaddingProperty, value);
        }

        /// <summary>
        /// Gets or sets the content displayed in the top command bar area.
        /// </summary>
        public object? TopCommandBar
        {
            get => GetValue(TopCommandBarProperty);
            set => SetValue(TopCommandBarProperty, value);
        }

        /// <summary>
        /// Gets or sets the content displayed in the bottom command bar area.
        /// </summary>
        public object? BottomCommandBar
        {
            get => GetValue(BottomCommandBarProperty);
            set => SetValue(BottomCommandBarProperty, value);
        }

        protected override AutomationPeer OnCreateAutomationPeer() => new ContentPageAutomationPeer(this);

        protected override Type StyleKeyOverride => typeof(ContentPage);

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            _contentPresenter = e.NameScope.Get<ContentPresenter>("PART_ContentPresenter");
            _topCommandBarPresenter = e.NameScope.Find<ContentPresenter>("PART_TopCommandBar");
            _bottomCommandBarPresenter = e.NameScope.Find<ContentPresenter>("PART_BottomCommandBar");

            UpdateCommandBars();
            UpdateContentSafeAreaPadding();
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == AutomaticallyApplySafeAreaPaddingProperty
                || change.Property == ContentProperty)
            {
                UpdateContentSafeAreaPadding();
            }
            else if (change.Property == TopCommandBarProperty
                     || change.Property == BottomCommandBarProperty)
            {
                UpdateCommandBars();
            }
        }

        protected override void UpdateContentSafeAreaPadding()
        {
            if (_contentPresenter != null)
            {
                _contentPresenter.Padding = AutomaticallyApplySafeAreaPadding
                    ? Padding.ApplySafeAreaPadding(SafeAreaPadding)
                    : Padding;
                _contentPresenter.InvalidateMeasure();

            }
        }

        private void ContentChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (e.OldValue is ILogical oldChild)
                LogicalChildren.Remove(oldChild);

            if (e.NewValue is ILogical newChild)
                LogicalChildren.Add(newChild);
        }

        private void UpdateCommandBars()
        {
            if (_topCommandBarPresenter != null)
            {
                _topCommandBarPresenter.Content = TopCommandBar;
                _topCommandBarPresenter.IsVisible = TopCommandBar != null;
            }

            if (_bottomCommandBarPresenter != null)
            {
                _bottomCommandBarPresenter.Content = BottomCommandBar;
                _bottomCommandBarPresenter.IsVisible = BottomCommandBar != null;
            }
        }
    }
}
