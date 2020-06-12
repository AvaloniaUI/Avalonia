using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Avalonia.Controls
{
    /// <summary>
    /// Formats a DateTimeOffset by the specified pattern
    /// Based on, but not replica, the Windows.Globalization.DateTimeFormatting.DateTimeFormatter
    /// https://docs.microsoft.com/en-us/uwp/api/windows.globalization.datetimeformatting.datetimeformatter?view=winrt-19041
    /// <para>
    /// Currently only the Gregorian Calendar is supported, and regions and languages are not supported. 
    /// Formatting timezones is also not supported, that's a large task
    /// </para>
    /// <para>
    /// /// Most formats used in UWP/WinUI are compatible here, but not all
    /// This DateTimeFormatter will also work with TimeSpans (only patterns though),
    /// in addition to the default DateTime/DateTimeOffset
    /// </para>
    /// <para>
    /// Formats are broken down into Patterns and Templates, which are "complete" patterns
    /// If a Template is used, only 1 may be provided and cannot be mixed with anything else
    /// Multiple patterns can be specified, and can be mixed with other text. All patterns
    /// must be enclosed in curly braces {}, e.g. {dayofweek}, or in xaml "{}{dayofweek}"
    /// NOTE: Formats and Templates are case sensitive
    /// </para>
    /// </summary>
    public sealed class DateTimeFormatter
    {
        public DateTimeFormatter(string formatString)
        {
            _format = formatString;
            var reg = new Regex("{([^{}]*)}");
            var results = reg.Matches(formatString).Cast<Match>().Select(m => m.Groups[1].Value).Distinct().ToList();
            Formats = results;
        }

        public string Clock
        {
            get
            {
                if (_clock == null)
                {
                    var timePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern;
                    if (timePattern.IndexOf("H") != -1)
                        return "24HourClock";
                    return "12HourClock";
                }
                return _clock;
            }
            set
            {
                _clock = value;
            }
        }
        public List<string> Formats { get; }

        /// <summary>
        /// Formats a DateTimeOffset object by the format of the <see cref="DateTimeFormatter"/>
        /// </summary>
        public string Format(DateTimeOffset toFormat)
        {
            string ret = _format;

            if (Formats.Count == 0)
            {
                return GetFormatTemplate(toFormat);
            }

            foreach (var item in Formats)
            {
                var p = $"{{{item}}}";
                var ns = Regex.Replace(ret, $"{{{Regex.Escape(item)}}}", GetFormatValue(item, toFormat));
                ret = ns;
            }

            return ret;
        }

        /// <summary>
        /// Formats a TimeSpan object by the format of the <see cref="DateTimeFormatter"/>
        /// </summary>
        public string Format(TimeSpan toFormat)
        {
            string ret = _format;

            foreach (var item in Formats)
            {
                var p = $"{{{item}}}";
                var ns = Regex.Replace(ret, $"{{{Regex.Escape(item)}}}", GetFormatValue(item, toFormat));
                ret = ns;
            }

            return ret;
        }

        private string GetFormatValue(string pattern, DateTimeOffset dt)
        {
            var sp = pattern.Split(new[] { "." }, StringSplitOptions.None);
            var type = sp[0].Trim();
            var desc = sp[1].Trim();
            var len = desc.Contains("(") ? int.Parse(desc.Substring(desc.IndexOf("(") + 1, desc.Length - desc.IndexOf(")"))) : -1;
            if (type.Equals("era"))
            {
                return "";
            }
            else if (type.Equals("year"))
            {
                var yr = dt.Year;
                if (len == -1)
                    return yr.ToString();
                else if (len <= 2)
                    return yr.ToString().Substring(2);
                else
                    return yr.ToString();
            }
            else if (type.Equals("month"))
            {
                var mon = dt.Month;
                var fmt = CultureInfo.CurrentCulture.DateTimeFormat;
                if (len == -1)
                    return desc == "full" ? fmt.GetMonthName(dt.Month) : fmt.GetAbbreviatedMonthName(dt.Month);
                var nm = desc == "full" ? fmt.GetMonthName(dt.Month) : fmt.GetAbbreviatedMonthName(dt.Month);
                len = Math.Min(nm.Length, Math.Max(0, len));
                return nm.Substring(0, len);
            }
            else if (type.Equals("dayofweek"))
            {
                var dow = dt.DayOfWeek;
                var fmt = CultureInfo.CurrentCulture.DateTimeFormat;
                if (len == -1)
                    return desc == "full" ? fmt.GetDayName(dt.DayOfWeek) : fmt.GetAbbreviatedDayName(dt.DayOfWeek);
                var nm = desc == "full" ? fmt.GetDayName(dt.DayOfWeek) : fmt.GetAbbreviatedDayName(dt.DayOfWeek);
                len = Math.Min(nm.Length, Math.Max(0, len));
                return nm.Substring(0, len);
            }
            else if (type.Equals("day"))
            {
                var dy = dt.Day;
                if (len == -1)
                    return dy.ToString();
                if (len < 1 || len > 2)
                    len = 2;
                return dy.ToString($"D{len}");
            }
            else if (type.Equals("period"))
            {
                if (Clock == "24HourClock")
                    return "";

                var hr = dt.Hour;
                if (hr >= 12)
                    return CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
                else
                    return CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
            }
            else if (type.Equals("hour"))
            {
                var shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.ToLower();
                var resolvedLength = 1;
                //valid Hours: h, hh
                if (shortTimePattern.Contains("hh"))
                {
                    resolvedLength = 2;
                }
                var hr = dt.Hour;

                if (Clock == "12HourClock")
                    hr = hr >= 13 ? hr - 12 : hr;

                if (len == -1)
                    return hr.ToString($"D{resolvedLength}");
                if (len < 1 || len > 2)
                    len = resolvedLength;

                return hr.ToString($"D{len}");
            }
            else if (type.Equals("minute"))
            {
                var shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.ToLower();
                var resolvedLength = 1;
                //valid mintues: m, mm
                if (shortTimePattern.Contains("hh"))
                {
                    resolvedLength = 2;
                }

                var dy = dt.Minute;
                if (len == -1)
                    return dy.ToString($"D{resolvedLength}");
                if (len < 1 || len > 2)
                    len = resolvedLength;

                return dy.ToString($"D{len}");
            }
            else if (type.Equals("second"))
            {
                var shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern.ToLower();
                var resolvedLength = 1;
                //valid seconds: s, ss
                if (shortTimePattern.Contains("ss"))
                {
                    resolvedLength = 2;
                }

                var dy = dt.Second;
                if (len == -1)
                    return dy.ToString($"D{resolvedLength}");
                if (len < 1 || len > 2)
                    len = resolvedLength;

                return dy.ToString($"D{len}");
            }
            else if (type.Equals("timezone"))
            {
                //Timezones aren't supported yet, that's a HUGE task
                //Multiple timezones can exist for a given offset from UTC,
                //So info about region is necessary to obtain it
                //It'd be nice if MS would move stuff from WinRT globalization namespace to .net
                throw new NotSupportedException("Timezones aren't supported yet");
            }

            throw new ArgumentException("Invalid format");

        }

        private string GetFormatValue(string pattern, TimeSpan ts)
        {
            var sp = pattern.Split(new[] { "." }, StringSplitOptions.None);
            var type = sp[0].Trim();
            var desc = sp.Count() > 1 ? sp[1].Trim() : "";
            var len = desc.Contains("(") ? int.Parse(desc.Substring(desc.IndexOf("(") + 1, desc.Length - desc.IndexOf(")"))) : -1;

            if (type.Equals("hour"))
            {
                var shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.ToLower();
                var resolvedLength = 1;
                //valid Hours: h, hh
                if (shortTimePattern.Contains("hh"))
                {
                    resolvedLength = 2;
                }
                var hr = ts.Hours;

                if (Clock == "12HourClock")
                    hr = hr >= 13 ? hr - 12 : hr;

                if (len == -1)
                    return hr.ToString($"D{resolvedLength}");
                if (len < 1 || len > 2)
                    len = resolvedLength;

                return hr.ToString($"D{len}");
            }
            else if (type.Equals("minute"))
            {
                var shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.ToLower();
                var resolvedLength = 1;
                //valid mintues: m, mm
                if (shortTimePattern.Contains("hh"))
                {
                    resolvedLength = 2;
                }

                var dy = ts.Minutes;
                if (len == -1)
                    return dy.ToString($"D{resolvedLength}");
                if (len < 1 || len > 2)
                    len = resolvedLength;

                return dy.ToString($"D{len}");
            }
            else if (type.Equals("second"))
            {
                var shortTimePattern = CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern.ToLower();
                var resolvedLength = 1;
                //valid seconds: s, ss
                if (shortTimePattern.Contains("ss"))
                {
                    resolvedLength = 2;
                }

                var dy = ts.Seconds;
                if (len == -1)
                    return dy.ToString($"D{resolvedLength}");
                if (len < 1 || len > 2)
                    len = resolvedLength;

                return dy.ToString($"D{len}");
            }
            else if (type.Equals("period"))
            {
                if (Clock == "24HourClock")
                    return "";

                var hr = ts.Hours;
                if (hr >= 12)
                    return CultureInfo.CurrentCulture.DateTimeFormat.PMDesignator;
                else
                    return CultureInfo.CurrentCulture.DateTimeFormat.AMDesignator;
            }

            throw new ArgumentException("Invalid format");

        }

        private string GetFormatTemplate(DateTimeOffset dt)
        {
            switch (_format)
            {
                case "longdate":
                    return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern);
                case "shortdate":
                    return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                case "longtime":
                    return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern);
                case "shorttime":
                    return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern);
                case "iso8601":
                case "sortable":
                    return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.SortableDateTimePattern);
                case "universalsortable":
                    return dt.ToString(CultureInfo.CurrentCulture.DateTimeFormat.UniversalSortableDateTimePattern);
                case "rfc1123":
                    return dt.ToUniversalTime().ToString(CultureInfo.CurrentCulture.DateTimeFormat.RFC1123Pattern);
            }
            throw new ArgumentException("Invalid template");
        }

        private string _format;
        private string _clock;
    }
}
