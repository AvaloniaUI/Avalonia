namespace Avalonia.Controls
{
    /// <summary>
    /// Defines the rule that determines the first week of the calendar year for week-number display.
    /// </summary>
    public enum CalendarWeekNumberRule
    {
        /// <summary>
        /// The first week of the year starts on the first day of the year and ends before the
        /// following designated first day of the week. Equivalent to
        /// <see cref="System.Globalization.CalendarWeekRule.FirstDay"/>.
        /// </summary>
        FirstDay = 0,

        /// <summary>
        /// The first week of the year begins on the first occurrence of the designated first day
        /// of the week on or after the first day of the year. Equivalent to
        /// <see cref="System.Globalization.CalendarWeekRule.FirstFullWeek"/>.
        /// </summary>
        FirstFullWeek = 1,

        /// <summary>
        /// The first week of the year is the first week with four or more days before the
        /// designated first day of the week. Equivalent to
        /// <see cref="System.Globalization.CalendarWeekRule.FirstFourDayWeek"/>.
        /// </summary>
        FirstFourDayWeek = 2,

        /// <summary>
        /// Uses ISO 8601 week numbering: the first week of the year must contain at least four days,
        /// and Monday is treated as the first day of the week, regardless of
        /// <see cref="Calendar.FirstDayOfWeek"/>. This is the most common rule in European locales
        /// and professional calendar applications.
        /// </summary>
        Iso = 3,
    }
}
