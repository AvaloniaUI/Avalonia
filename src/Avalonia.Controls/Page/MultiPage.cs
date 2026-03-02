using System;
using System.Collections;
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
        private IEnumerable? _pages;

        /// <summary>
        /// Defines the <see cref="Pages"/> property.
        /// </summary>
        public static readonly DirectProperty<MultiPage, IEnumerable?> PagesProperty =
            AvaloniaProperty.RegisterDirect<MultiPage, IEnumerable?>(nameof(Pages),
                o => o.Pages, (o, v) => o.Pages = v);

        /// <summary>
        /// Defines the <see cref="PageTemplate"/> property.
        /// </summary>
        public static readonly StyledProperty<IDataTemplate?> PageTemplateProperty =
            AvaloniaProperty.Register<MultiPage, IDataTemplate?>(nameof(PageTemplate), new DefaultPageDataTemplate());

        /// <summary>
        /// Gets or sets the collection of child pages.
        /// </summary>
        [Content]
        public virtual IEnumerable? Pages
        {
            get => _pages;
            set => SetAndRaise(PagesProperty, ref _pages, value);
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

                if (change.NewValue != null)
                {
                    foreach (var page in Pages!)
                    {
                        if (page is ILogical logical)
                            LogicalChildren.Add(logical);
                    }
                }

                if (change.NewValue is INotifyCollectionChanged newNotifyCollection)
                    newNotifyCollection.CollectionChanged += NotifyCollection_CollectionChanged;

                UpdateActivePage();
            }
            else if (change.Property == CurrentPageProperty)
                CurrentPageChanged?.Invoke(this, EventArgs.Empty);
        }

        private void NotifyCollection_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:
                    LogicalChildren.Clear();
                    if (_pages != null)
                    {
                        foreach (var item in _pages)
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
            UpdateActivePage();
        }
    }
}
