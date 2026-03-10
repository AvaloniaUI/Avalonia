using System;
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
                if (change.OldValue is INotifyCollectionChanged oldNotifyCollection)
                    oldNotifyCollection.CollectionChanged -= NotifyCollection_CollectionChanged;

                LogicalChildren.Clear();

                if (change.NewValue is IEnumerable<Page> newPages)
                {
                    foreach (var page in newPages)
                    {
                        if (page is ILogical logical)
                            LogicalChildren.Add(logical);
                    }
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
                        foreach (var item in Pages)
                        {
                            if (item is ILogical logical)
                                LogicalChildren.Add(logical);
                        }
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                case NotifyCollectionChangedAction.Replace:
                    if (e.OldItems != null)
                    {
                        foreach (var old in e.OldItems)
                        {
                            if (old is ILogical logical)
                                LogicalChildren.Remove(logical);
                        }
                    }

                    if (e.NewItems != null)
                    {
                        int insertIdx = e.NewStartingIndex >= 0 ? e.NewStartingIndex : LogicalChildren.Count;
                        foreach (var newItem in e.NewItems)
                        {
                            if (newItem is ILogical logical)
                                LogicalChildren.Insert(insertIdx++, logical);
                        }
                    }
                    break;

                default:
                    if (e.OldItems != null)
                    {
                        foreach (var old in e.OldItems)
                        {
                            if (old is ILogical logical)
                                LogicalChildren.Remove(logical);
                        }
                    }

                    if (e.NewItems != null)
                    {
                        int insertIdx = e.NewStartingIndex >= 0 ? e.NewStartingIndex : LogicalChildren.Count;
                        foreach (var newItem in e.NewItems)
                        {
                            if (newItem is ILogical logical)
                                LogicalChildren.Insert(insertIdx++, logical);
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
                            if (ReferenceEquals(old, current)) { currentRemoved = true; break; }
                }
                else if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    currentRemoved = true;
                    if (Pages != null)
                        foreach (var item in Pages)
                            if (ReferenceEquals(item, current)) { currentRemoved = false; break; }
                }

                if (currentRemoved)
                    navType = NavigationType.Remove;
            }

            UpdateActivePage(navType);
        }
    }
}
