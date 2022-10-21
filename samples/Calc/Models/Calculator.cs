using System;
using System.Globalization;

namespace Calc.Models;

public static class Calculator
{
    public static string Calculate(string str)
    {
        var calculus = new MyStringBuilder(str);

        var numberOfOpeningParentheses = calculus.Count('(');
        var numberOfClosingParentheses = calculus.Count(')');
        
        if (numberOfOpeningParentheses != numberOfClosingParentheses)
            return "Waiting until all parentheses are closed";
        
        CalculateParentheses(ref calculus);
        CalculateNonParentheses(ref calculus);

        return calculus.ToString();
    }

    private static Operator? CharToOperator(char? character)
    {
        return character switch
        {
            OperatorChar.Add => Operator.Add,
            OperatorChar.Subtract => Operator.Subtract,
            OperatorChar.Multiply => Operator.Multiply,
            OperatorChar.Divide => Operator.Divide,
            _ => null
        };
    }

    private static void CalculateNonParentheses(ref MyStringBuilder calculus)
    {
        int indexOfOperator;
        
        // Search calculations with precedence first. When there isn't more, continue with the others
        while ((indexOfOperator = calculus.IndexOfAny(OperatorChar.PrecedentOperators, 1)) > 0 ||
               (indexOfOperator = calculus.IndexOfAny(OperatorChar.NonPrecedentOperators, 1)) > 0)
        {
            // ==== Find the first operand ==== //
            var indexOfPreviousOperator = SetIndexOfPreviousOperator(calculus, indexOfOperator);
            var stringOfFirstValue = calculus[(indexOfPreviousOperator + 1)..indexOfOperator];
            var startIndexOfCalculation = indexOfPreviousOperator + 1;

            // First value could be just the sign -, e.g. in --3
            if (stringOfFirstValue.Length == 1 &&
                OperatorChar.IsAnOperator(stringOfFirstValue[0]))
            {
                stringOfFirstValue = "0";
                indexOfOperator--; // This way operator would be - and secondValue -3
            }
            
            // ==== Find the second operand ==== //
            // startIndex = indexOfOperator + 2 avoids to detect sign of second value as operator
            var indexOfNextOperator = calculus.IndexOfAny(OperatorChar.Operators, indexOfOperator + 2);

            if (indexOfNextOperator == -1) // Last calculation
                indexOfNextOperator = calculus.Length;
            
            var stringOfSecondValue = calculus[(indexOfOperator + 1)..indexOfNextOperator];
            var nextIndexAfterCalculation = indexOfNextOperator;

            // ==== Construct the calculation ==== //
            var firstValue = Convert.ToDouble(stringOfFirstValue);
            var @operator = CharToOperator(calculus[indexOfOperator]);
            var secondValue = Convert.ToDouble(stringOfSecondValue);

            var calculation = new Calculation(firstValue, secondValue, @operator);
            
            // Replace calculation with its result
            calculus.Replace(startIndexOfCalculation, nextIndexAfterCalculation,
                Convert.ToString(calculation.Calculate(), CultureInfo.CurrentCulture));
        }
    }

    private static void CalculateParentheses(ref MyStringBuilder calculus)
    {
        int indexOfOpeningParenthesis;
        while ((indexOfOpeningParenthesis = calculus.LastIndexOf('(')) != -1)
        {
            var indexOfClosingParenthesis = calculus.IndexOf(')', indexOfOpeningParenthesis);
            // Replace parentheses with its result
            calculus.Replace(indexOfOpeningParenthesis, indexOfClosingParenthesis + 1,
                Calculate(calculus[(indexOfOpeningParenthesis + 1)..indexOfClosingParenthesis]));
        }
    }

    private static int SetIndexOfPreviousOperator(MyStringBuilder calculus, int indexOfOperator)
    {
        var indexOfPreviousOperator = calculus.LastIndexOfAny(OperatorChar.Operators, indexOfOperator - 1);
        
        // First calculation. There could not be an operator at the beginning, it must be a sign
        if (indexOfPreviousOperator == 0)
        {
            indexOfPreviousOperator = -1;
        }
        // If the first value is negative and not the first calculation, an operator must be just before the index
        // previously calculated as the indexOfPreviousOperator, e.g. in a+-b/c the - isn't previousOperator, it's +
        else if (indexOfPreviousOperator > 0 &&
                 calculus[indexOfPreviousOperator].Equals(OperatorChar.Subtract) && // minus sign
                 OperatorChar.IsAnOperator(calculus[indexOfPreviousOperator - 1])) // previous index contains an operator
        {
            indexOfPreviousOperator--;
        }
        
        return indexOfPreviousOperator;
    }
}