// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Avalonia.Controls
{
    /// <summary>
    /// Specifies date formats for a
    /// <see cref="T:Avalonia.Controls.CalendarDatePicker" />.
    /// </summary>
    public enum CalendarDatePickerFormat
    {
        /// <summary>
        /// Specifies that the date should be displayed using unabbreviated days
        /// of the week and month names.
        /// </summary>
        Long = 0,

        /// <summary>
        /// Specifies that the date should be displayed using abbreviated days
        /// of the week and month names.
        /// </summary>
        Short = 1,

        /// <summary>
        /// Specifies that the date should be displayed using a custom format string.
        /// </summary>
        Custom = 2
    }
}
