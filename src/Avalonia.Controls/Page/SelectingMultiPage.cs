using System;

namespace Avalonia.Controls
{
    /// <summary>
    /// Base class for multi-page controls with index-based selection.
    /// </summary>
    public abstract class SelectingMultiPage : MultiPage
    {
        /// <summary>
        /// Defines the <see cref="SelectedIndex"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingMultiPage, int> SelectedIndexProperty =
            AvaloniaProperty.RegisterDirect<SelectingMultiPage, int>(
                nameof(SelectedIndex),
                o => o._selectedIndex,
                (o, v) => o.SelectedIndex = v);

        /// <summary>
        /// Defines the <see cref="SelectedPage"/> property.
        /// </summary>
        public static readonly DirectProperty<SelectingMultiPage, Page?> SelectedPageProperty =
            AvaloniaProperty.RegisterDirect<SelectingMultiPage, Page?>(
                nameof(SelectedPage),
                o => o._selectedPage);

        private int _selectedIndex = -1;
        private Page? _selectedPage;

        /// <summary>
        /// Gets or sets the zero-based index of the selected page.
        /// </summary>
        public int SelectedIndex
        {
            get => _selectedIndex;
            set => ApplySelectedIndex(value);
        }

        /// <summary>
        /// Gets the currently selected page, or <see langword="null"/> if no page is selected.
        /// </summary>
        public Page? SelectedPage => _selectedPage;

        /// <summary>
        /// Raised when the selected page changes.
        /// </summary>
        public event EventHandler<PageSelectionChangedEventArgs>? SelectionChanged;

        /// <summary>
        /// Commits a selection change and fires lifecycle events on the outgoing and incoming pages.
        /// </summary>
        protected void CommitSelection(int newIndex, Page? newPage)
        {
            var previousPage = _selectedPage;
            SetAndRaise(SelectedIndexProperty, ref _selectedIndex, newIndex);
            SetAndRaise(SelectedPageProperty, ref _selectedPage, newPage);
            SetCurrentValue(CurrentPageProperty, newPage);
            if (!ReferenceEquals(previousPage, newPage))
            {
                SelectionChanged?.Invoke(this, new PageSelectionChangedEventArgs(previousPage, newPage));

                if (previousPage != null)
                {
                    previousPage.SendDisappearing();
                    previousPage.SendNavigatedFrom(new NavigatedFromEventArgs(newPage, NavigationType.Replace));
                }

                if (newPage != null)
                {
                    newPage.SendNavigatedTo(new NavigatedToEventArgs(previousPage, NavigationType.Replace));
                    newPage.SendAppearing();
                }
            }
        }

        /// <summary>
        /// Applies the requested <paramref name="index"/> change.
        /// </summary>
        protected abstract void ApplySelectedIndex(int index);

        /// <summary>
        /// Stores the selected index without routing through a child control.
        /// </summary>
        protected void StoreSelectedIndex(int index)
        {
            SetAndRaise(SelectedIndexProperty, ref _selectedIndex, index);
        }
    }
}
