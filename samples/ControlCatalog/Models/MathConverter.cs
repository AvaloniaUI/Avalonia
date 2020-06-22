using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ControlCatalog.Models
{
    public class MathConverter : IValueConverter
    {
        private static readonly List<char> _allOperators = new List<char> { '+', '-', '*', '/', '%', '(', ')' };

        private static readonly List<string> _grouping = new List<string> { "(", ")" };
        private static readonly List<string> _operators = new List<string> { "+", "-", "*", "/", "%" };

        private static bool TryParseDouble(string s, out double number) =>
            double.TryParse(s, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out number);

        // Parse value into equation and remove spaces
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(parameter is string mathEquation))
                return null;

            mathEquation = mathEquation.Replace(" ", string.Empty);
            mathEquation = mathEquation.Replace("@VALUE", value.ToString());

            // Validate values and get list of numbers in equation
            var numbers = new List<double>();
            foreach (var s in mathEquation.Split(_allOperators.ToArray()))
            {
                if (s == string.Empty)
                    continue;

                if (TryParseDouble(s, out var number))
                {
                    numbers.Add(number);
                }
                else
                {
                    // Handle Error - Some non-numeric, operator, or grouping character found in string
                    throw new InvalidCastException();
                }
            }

            // Begin parsing method
            EvaluateMathString(mathEquation, numbers, 0);

            // After parsing the numbers list should only have one value - the total
            return numbers[0];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        // Evaluates a mathematical string and keeps track of the results in a List<double> of numbers
        private void EvaluateMathString(string mathEquation, List<double> numbers, int index)
        {
            // Loop through each mathematical token in the equation
            string token = GetNextToken(mathEquation);
            while (token != string.Empty)
            {
                // Remove token from mathEquation
                mathEquation = mathEquation.Remove(0, token.Length);

                // If token is a grouping character, it affects program flow
                if (_grouping.Contains(token))
                {
                    switch (token)
                    {
                        case "(":
                            EvaluateMathString(mathEquation, numbers, index);
                            break;

                        case ")":
                            return;
                    }
                }

                // If token is an operator, do requested operation
                if (_operators.Contains(token))
                {
                    // If next token after operator is a parenthesis, call method recursively
                    string nextToken = GetNextToken(mathEquation);
                    if (nextToken == "(")
                        EvaluateMathString(mathEquation, numbers, index + 1);

                    // Verify that enough numbers exist in the List<double> to complete the operation
                    // and that the next token is either the number expected, or it was a ( meaning
                    // that this was called recursively and that the number changed
                    if (numbers.Count > index + 1
                        && (TryParseDouble(nextToken, out var number) && number == numbers[index + 1]
                            || nextToken == "("))
                    {
                        switch (token)
                        {
                            case "+":
                                numbers[index] = numbers[index] + numbers[index + 1];
                                break;
                            case "-":
                                numbers[index] = numbers[index] - numbers[index + 1];
                                break;
                            case "*":
                                numbers[index] = numbers[index] * numbers[index + 1];
                                break;
                            case "/":
                                numbers[index] = numbers[index] / numbers[index + 1];
                                break;
                            case "%":
                                numbers[index] = numbers[index] % numbers[index + 1];
                                break;
                        }
                        numbers.RemoveAt(index + 1);
                    }
                    else
                    {
                        // Handle Error - Next token is not the expected number
                        throw new FormatException("Next token is not the expected number");
                    }
                }

                token = GetNextToken(mathEquation);
            }
        }

        // Gets the next mathematical token in the equation
        private string GetNextToken(string mathEquation)
        {
            // If we're at the end of the equation, return string.empty
            if (string.IsNullOrEmpty(mathEquation))
                return string.Empty;

            // Get next operator or numeric value in equation and return it
            var result = string.Empty;
            foreach (char c in mathEquation)
            {
                if (_allOperators.Contains(c))
                    return result == string.Empty ? c.ToString() : result;
                result += c;
            }

            return result;
        }
    }
}
