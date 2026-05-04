namespace Avalonia.Controls.Primitives
{
    /// <summary>
    /// Displays a week number in the month view of a <see cref="Calendar"/>.
    /// Apply styles targeting <see cref="CalendarWeekNumberLabel"/> to customise
    /// the appearance — for example <c>FontWeight="Bold"</c>.
    /// Use the <c>:header</c> pseudo-class to target the column header cell (row 0).
    /// </summary>
    public sealed class CalendarWeekNumberLabel : ContentControl
    {
        private bool _isHeader;

        /// <summary>
        /// Gets or sets a value indicating whether this label is the column header cell
        /// (placed in row 0 of the month grid, above the week-number data cells).
        /// Themes can target this with the <c>:header</c> pseudo-class.
        /// </summary>
        public bool IsHeader
        {
            get => _isHeader;
            internal set
            {
                if (_isHeader == value) return;
                _isHeader = value;
                PseudoClasses.Set(":header", value);
            }
        }
    }
}
