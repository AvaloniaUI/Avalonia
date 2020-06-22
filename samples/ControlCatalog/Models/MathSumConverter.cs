using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ControlCatalog.Models
{
    public class MathSumConverter : IValueConverter
    {
        private const string SumOperationsOpen = "sum(";
        
        private const string SumOperationsClose = ")";

        private const char SeparateSymbol = ';';

        private static bool TryParseDouble(string s, out double number) =>
            double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out number);

        // Parse value into equation and remove spaces
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string mathEquation))
                return null;

            mathEquation = mathEquation.Trim();
            var isValid = mathEquation.StartsWith(SumOperationsOpen, StringComparison.OrdinalIgnoreCase)
                       && mathEquation.EndsWith(SumOperationsClose, StringComparison.Ordinal);
            if (!isValid)
                throw new InvalidOperationException();

            var numbers = mathEquation
                .Substring(SumOperationsOpen.Length, mathEquation.Length - SumOperationsOpen.Length - 1)
                .Replace("@VALUE", value.ToString())
                .Split(SeparateSymbol);

            var result = 0.0;
            foreach (var x in numbers)
            {
                if (!TryParseDouble(x, out var number))
                    throw new InvalidCastException();

                result += number;
            }

            return result;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
