using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia.Controls.Templates;
using Avalonia.LogicalTree;
using Avalonia.Metadata;

namespace Avalonia.Controls
{
    /// <summary>
    /// Abstract base class for pages that host a collection of child pages.
    /// </summary>
    public abstract class MultiPage : Page
    {
        /// <summary>
        /// Defines the <see cref="Pages"/> property.
        /// </summary>
        public static readonly StyledProperty<IEnumerable<Page>?> PagesProperty =
            AvaloniaProperty.Register<MultiPage, IEnumerable<Page>?>(nameof(Pages));

        /// <summary>
        /// Defines the <see cref="ItemsSource"/> property.
        /// </summary>
        public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
            AvaloniaProperty.Register<MultiPage, IEnumerable?>(nameof(ItemsSource));

        /// <summary>
        /// Defines the <see cref="PageTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> PageTemplateProperty =
            AvaloniaProperty.Register<MultiPage, IDataTemplate?>(nameof(PageTemplate), new DefaultPageDataTemplate());

        /// <summary>
        /// Gets or sets the collection of child pages.
        /// </summary>
        /// <remarks>
        /// If the assigned collection implements <see cref="INotifyCollectionChanged"/>,
        /// this control subscribes to its <c>CollectionChanged</c> event with a strong reference.
        /// The subscription is released when a new collection is assigned or when the control
        /// is detached from the visual tree. Avoid keeping the collection alive longer than
        /// the control to prevent the control from being retained by the collection.
        /// </remarks>
        [Content]
        public IEnumerable<Page>? Pages
        {
            get => GetValue(PagesProperty);
            set => SetValue(PagesProperty, value);
        }

        /// <summary>
        /// Gets or sets a view-model collection to bind to. Use together with
        /// <see cref="PageTemplate"/> to convert each item into a <see cref="Page"/>.
        /// When set, takes precedence over <see cref="Pages"/> as the item source for
        /// the inner tab or carousel control.
        /// </summary>
        public IEnumerable? ItemsSource
        {
            get => GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// Gets or sets the data template used to create pages from view-model items.
        /// </summary>
        public IDataTemplate? PageTemplate
        {
            get => GetValue(PageTemplateProperty);
            set => SetValue(PageTemplateProperty, value);
        }

        /// <summary>
        /// Occurs when the <see cref="Page.CurrentPage"/> property changes.
        /// </summary>
        public event EventHandler? CurrentPageChanged;

        /// <summary>
        /// Occurs when the <see cref="Pages"/> collection changes.
        /// </summary>
        public event EventHandler<NotifyCollectionChangedEventArgs>? PagesChanged;

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == PagesProperty)
            {
                if (ReferenceEquals(change.OldValue, change.NewValue))
                    return;

                if (change.OldValue is INotifyCollectionChanged oldNotifyCollection)
                    oldNotifyCollection.CollectionChanged -= NotifyCollection_CollectionChanged;

                LogicalChildren.Clear();

                if (change.NewValue is IEnumerable<Page> newItems)
                {
                    foreach (var page in newItems)
                        LogicalChildren.Add(page);
                }

                if (change.NewValue is INotifyCollectionChanged newNotifyCollection)
                    newNotifyCollection.CollectionChanged += NotifyCollection_CollectionChanged;

                if (change.NewValue != null)
                    UpdateActivePage();
                else
                    SetCurrentValue(CurrentPageProperty, null);
            }
            else if (change.Property == CurrentPageProperty)
                CurrentPageChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Sealed: routes no-argument calls to <see cref="UpdateActivePage(NavigationType)"/>
        /// using <see cref="NavigationType.Replace"/> as the default. Subclasses must override
        /// the typed overload, not this method.
        /// </summary>
        protected sealed override void UpdateActivePage() => UpdateActivePage(NavigationType.Replace);

        /// <summary>
        /// Called when the active child page changes.
        /// </summary>
        /// <param name="navigationType">The reason for the page change.</param>
        /// <remarks>
        /// The base class calls this method in the following situations, passing the corresponding
        /// <see cref="NavigationType"/> value:
        /// <list type="bullet">
        ///   <item><description>
        ///     <see cref="NavigationType.Replace"/>. The <see cref="Pages"/> collection was
        ///     assigned, a new item was added, or the active page changed for any reason not
        ///     covered below.
        ///   </description></item>
        ///   <item><description>
        ///     <see cref="NavigationType.Remove"/>. The currently active page was removed from
        ///     the <see cref="Pages"/> collection or the collection was reset and the active page
        ///     is no longer present. Subclasses should select a replacement page.
        ///   </description></item>
        /// </list>
        /// Subclasses may also call this method directly with any <see cref="NavigationType"/>
        /// value appropriate for their own navigation model.
        /// </remarks>
        protected virtual void UpdateActivePage(NavigationType navigationType) { }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);

            if (Pages is INotifyCollectionChanged collection)
            {
                collection.CollectionChanged -= NotifyCollection_CollectionChanged;
                collection.CollectionChanged += NotifyCollection_CollectionChanged;
            }
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);

            if (Pages is INotifyCollectionChanged collection)
                collection.CollectionChanged -= NotifyCollection_CollectionChanged;
        }

        private void NotifyCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    LogicalChildren.Clear();
                    if (Pages != null)
                    {
                        foreach (var page in Pages)
                            LogicalChildren.Add(page);
                    }
                    break;

                default:
                    if (e.OldItems != null)
                    {
                        foreach (var old in e.OldItems)
                        {
                            if (old is Page page)
                                LogicalChildren.Remove(page);
                        }
                    }

                    if (e.NewItems != null)
                    {
                        int insertIdx = e.NewStartingIndex >= 0 ? e.NewStartingIndex : LogicalChildren.Count;
                        foreach (var newItem in e.NewItems)
                        {
                            if (newItem is Page page)
                                LogicalChildren.Insert(insertIdx++, page);
                        }
                    }
                    break;
            }

            PagesChanged?.Invoke(this, e);

            var navType = NavigationType.Replace;
            var current = CurrentPage;
            if (current != null)
            {
                bool currentRemoved = false;
                if (e.Action == NotifyCollectionChangedAction.Remove ||
                    e.Action == NotifyCollectionChangedAction.Replace)
                {
                    if (e.OldItems != null)
                        foreach (var old in e.OldItems)
                            if (ReferenceEquals(old, current))
                            {
                                currentRemoved = true;
                                break;
                            }
                }
                else if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    currentRemoved = true;
                    if (Pages != null)
                        foreach (var page in Pages)
                            if (ReferenceEquals(page, current))
                            {
                                currentRemoved = false;
                                break;
                            }
                }

                if (currentRemoved)
                    navType = NavigationType.Remove;
            }

            UpdateActivePage(navType);
        }
    }
}
