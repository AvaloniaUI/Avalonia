using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Avalonia.Controls.MaskedTextBox.Filters
{
    public class RegExFilter : DefaultFilter
    {
        public static readonly RegExFilter UNumberFilter = new(@"^\d*$");
        public static readonly RegExFilter NumberFilter = new(@"^-?\d*$");
        public static readonly RegExFilter UDecimalFilter = new(@"^\d*([.,]\d*)?$");
        public static readonly RegExFilter DecimalFilter = new(@"^-?\d*([\.,]\d*)?$");

        public int? MaxLength { get; }

        private readonly Regex _regEx;

        public RegExFilter(string regExp, int? maxLength = null)
        {
            _regEx = string.IsNullOrEmpty(regExp) ? null : new Regex(regExp);
            AddTextValidator(CheckRegExp);
            MaxLength = maxLength;
            AddTextValidator(CheckMaxLength);
        }

        public RegExFilter(int? maxLength) : this(string.Empty, maxLength) { }

        private bool CheckRegExp(string newText)
        {
            return _regEx is null || _regEx.Match(newText).Success;
        }

        private bool CheckMaxLength(string newText)
        {
            return !MaxLength.HasValue || MaxLength.Value >= newText.Length;
        }
    }
}
