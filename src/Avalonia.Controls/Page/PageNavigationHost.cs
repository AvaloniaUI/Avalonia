using Avalonia.Controls.Platform;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;

namespace Avalonia.Controls
{
    /// <summary>
    /// A content control that hosts a root <see cref="Page"/>, wires up safe-area insets,
    /// and forwards the system back-button event into the page tree.
    /// </summary>
    public class PageNavigationHost : ContentControl
    {
        private TopLevel? _topLevel;
        private IInsetsManager? _insetManager;
        private ContentPresenter? _contentPresenter;

        /// <summary>
        /// Defines the <see cref="Page"/> property.
        /// </summary>
        public static readonly StyledProperty<Page?> PageProperty =
            AvaloniaProperty.Register<PageNavigationHost, Page?>(nameof(Page));

        protected override System.Type StyleKeyOverride => typeof(ContentControl);

        static PageNavigationHost()
        {
            ContentTemplateProperty.OverrideDefaultValue<PageNavigationHost>(new DefaultPageDataTemplate());
            TopLevel.AutoSafeAreaPaddingProperty.OverrideDefaultValue<PageNavigationHost>(false);
        }

        /// <summary>
        /// Gets or sets the root page displayed by this host.
        /// </summary>
        public Page? Page
        {
            get => GetValue(PageProperty);
            set => SetValue(PageProperty, value);
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            CleanUpSubscriptions();

            _topLevel = TopLevel.GetTopLevel(this);
            _insetManager = _topLevel?.InsetsManager;

            if (_insetManager != null)
            {
                _insetManager.SafeAreaChanged += InsetManager_SafeAreaChanged;
                _insetManager.DisplayEdgeToEdgePreference = true;
            }

            if (_topLevel != null)
            {
                _topLevel.BackRequested += TopLevel_BackRequested;
                _topLevel.ScalingChanged += TopLevel_ScalingChanged;
            }

            AttachContentPresenter();
        }

        private void TopLevel_ScalingChanged(object? sender, System.EventArgs e)
        {
            if (_insetManager != null && _contentPresenter?.Child is Page page)
                page.SafeAreaPadding = _insetManager.SafeAreaPadding;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            CleanUpSubscriptions();

            if (_contentPresenter != null)
                _contentPresenter.PropertyChanged -= ContentPresenter_PropertyChanged;
        }

        private void CleanUpSubscriptions()
        {
            if (_insetManager != null)
            {
                _insetManager.SafeAreaChanged -= InsetManager_SafeAreaChanged;
                _insetManager = null;
            }

            if (_topLevel != null)
            {
                _topLevel.BackRequested -= TopLevel_BackRequested;
                _topLevel.ScalingChanged -= TopLevel_ScalingChanged;
                _topLevel = null;
            }
        }

        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {
            base.OnApplyTemplate(e);

            if (_contentPresenter != null)
                _contentPresenter.PropertyChanged -= ContentPresenter_PropertyChanged;

            _contentPresenter = e.NameScope.Get<ContentPresenter>("PART_ContentPresenter");

            AttachContentPresenter();
        }

        private void AttachContentPresenter()
        {
            if (_contentPresenter == null)
                return;

            if (_insetManager != null && _contentPresenter.Child is Page page)
                page.SafeAreaPadding = _insetManager.SafeAreaPadding;

            if (IsAttachedToVisualTree)
                _contentPresenter.PropertyChanged += ContentPresenter_PropertyChanged;
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PageProperty)
            {
                var oldPage = change.GetOldValue<Page?>();
                var newPage = change.GetNewValue<Page?>();

                SetCurrentValue(ContentProperty, newPage);

                if (_insetManager != null && newPage != null)
                    newPage.SafeAreaPadding = _insetManager.SafeAreaPadding;

                oldPage?.SendNavigatedFrom(new NavigatedFromEventArgs(newPage, NavigationType.Replace));
                newPage?.SendNavigatedTo(new NavigatedToEventArgs(oldPage, NavigationType.Replace));
            }
        }

        private void ContentPresenter_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property != ContentPresenter.ChildProperty)
                return;

            if (e.OldValue is Page oldPage)
                oldPage.SafeAreaPadding = default;

            if (e.NewValue is Page newPage && _insetManager != null)
                newPage.SafeAreaPadding = _insetManager.SafeAreaPadding;
        }

        private void TopLevel_BackRequested(object? sender, RoutedEventArgs e)
        {
            if (e.Handled)
                return;

            if (Presenter?.Child is Page page)
            {
                var forwarded = new RoutedEventArgs(Page.PageNavigationSystemBackButtonPressedEvent);
                page.RaiseEvent(forwarded);
                e.Handled = forwarded.Handled;
            }
        }

        private void InsetManager_SafeAreaChanged(object? sender, SafeAreaChangedArgs e)
        {
            if (Content != null && Presenter?.Child is Page page)
                page.SafeAreaPadding = e.SafeAreaPadding;
        }
    }
}
