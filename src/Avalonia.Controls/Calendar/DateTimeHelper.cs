// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see https://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Avalonia.Controls
{
    internal static class DateTimeHelper
    {
        public static DateTime? AddDays(DateTime time, int days)
        {
            System.Globalization.Calendar cal = new GregorianCalendar();
            try
            {
                return cal.AddDays(time, days);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static DateTime? AddMonths(DateTime time, int months)
        {
            System.Globalization.Calendar cal = new GregorianCalendar();
            try
            {
                return cal.AddMonths(time, months);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static DateTime? AddYears(DateTime time, int years)
        {
            System.Globalization.Calendar cal = new GregorianCalendar();
            try
            {
                return cal.AddYears(time, years);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        public static int CompareDays(DateTime dt1, DateTime dt2)
        {
            return DateTime.Compare(DiscardTime(dt1), DiscardTime(dt2));
        }

        public static int CompareYearMonth(DateTime dt1, DateTime dt2)
        {
            return (dt1.Year - dt2.Year) * 12 + (dt1.Month - dt2.Month);
        }

        public static int DecadeOfDate(DateTime date)
        {
            return date.Year - (date.Year % 10);
        }

        public static DateTime DiscardDayTime(DateTime d)
        {
            return new DateTime(d.Year, d.Month, 1, 0, 0, 0);
        }

        public static DateTime DiscardTime(DateTime d)
        {
            return d.Date;
        }

        public static int EndOfDecade(DateTime date)
        {
            return DecadeOfDate(date) + 9;
        }

        public static DateTimeFormatInfo GetCurrentDateFormat()
        {
            if (CultureInfo.CurrentCulture.Calendar is GregorianCalendar)
            {
                return CultureInfo.CurrentCulture.DateTimeFormat;
            }
            else
            {
                foreach (System.Globalization.Calendar cal in CultureInfo.CurrentCulture.OptionalCalendars)
                {
                    if (cal is GregorianCalendar)
                    {
                        // if the default calendar is not Gregorian, return the
                        // first supported GregorianCalendar dtfi
                        DateTimeFormatInfo dtfi = new CultureInfo(CultureInfo.CurrentCulture.Name).DateTimeFormat;
                        dtfi.Calendar = cal;
                        return dtfi;
                    }
                }

                // if there are no GregorianCalendars in the OptionalCalendars
                // list, use the invariant dtfi
                DateTimeFormatInfo dt = new CultureInfo(CultureInfo.InvariantCulture.Name).DateTimeFormat;
                dt.Calendar = new GregorianCalendar();
                return dt;
            }
        }
        public static bool InRange(DateTime date, CalendarDateRange range)
        {
            Debug.Assert(DateTime.Compare(range.Start, range.End) < 1, "The range should start before it ends!");

            if (CompareDays(date, range.Start) > -1 && CompareDays(date, range.End) < 1)
            {
                return true;
            }

            return false;
        }

        public static string ToYearMonthPatternString(DateTime date)
        {
            var format = GetCurrentDateFormat();
            return date.ToString(format.YearMonthPattern, format);
        }

        public static string ToYearString(DateTime date)
        {
            var format = GetCurrentDateFormat();
            return date.Year.ToString(format);
        }

        /// <summary>
        /// Gets the week-of-year number for the specified date.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <param name="rule">The rule that defines what constitutes the first week of the year.</param>
        /// <param name="firstDayOfWeek">The first day of the week. Ignored when <paramref name="rule"/> is <see cref="CalendarWeekNumberRule.Iso"/>.</param>
        /// <returns>The week number that <paramref name="date"/> falls in.</returns>
        public static int GetWeekOfYear(DateTime date, CalendarWeekNumberRule rule, DayOfWeek firstDayOfWeek)
        {
            var mappedRule = (CalendarWeekRule)(int)rule;

            // Iso is an explicit shorthand for ISO 8601 (FirstFourDayWeek + Monday).
            // Also catches FirstFourDayWeek + Monday explicitly, because .NET's
            // Calendar.GetWeekOfYear incorrectly returns week 53 for late-December dates
            // that ISO 8601 assigns to week 1 of the next year (e.g. 2018-12-31).
            if (rule == CalendarWeekNumberRule.Iso ||
                (mappedRule == CalendarWeekRule.FirstFourDayWeek && firstDayOfWeek == DayOfWeek.Monday))
                return ISOWeek.GetWeekOfYear(date);

            return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date, mappedRule, firstDayOfWeek);
        }
    }
}
