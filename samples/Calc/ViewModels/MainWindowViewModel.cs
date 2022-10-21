using System;
using System.Globalization;
using System.IO;
using System.Reactive;
using Calc.Models;
using ReactiveUI;

namespace Calc.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private string _shownString = string.Empty;
        private string _shownResult = string.Empty;
        private int _numberOfOpeningParentheses;
        private int _numberOfClosingParentheses;
        
        // Commands
        public ReactiveCommand<Unit, Unit> AddDecimalSeparatorCommand { get; }
        public ReactiveCommand<int, Unit> AddNumberCommand { get; }
        public ReactiveCommand<Operator, Unit> AddOperatorCommand { get; }
        public ReactiveCommand<Unit, Unit> AddParenthesisCommand { get; }
        public ReactiveCommand<Unit, Unit> AlternateNegativePositiveCommand { get; }
        public ReactiveCommand<Unit, Unit> ClearScreenCommand { get; }
        public ReactiveCommand<Unit, Unit> DeleteLastCommand { get; }
        public ReactiveCommand<Unit, Unit> PickResultCommand { get; }

        public MainWindowViewModel()
        {
            AddDecimalSeparatorCommand = ReactiveCommand.Create(AddDecimalSeparator);
            AddNumberCommand = ReactiveCommand.Create<int>(AddNumber);
            AddOperatorCommand = ReactiveCommand.Create<Operator>(AddOperator);
            AddParenthesisCommand = ReactiveCommand.Create(AddParenthesis);
            AlternateNegativePositiveCommand = ReactiveCommand.Create(AlternateNegativePositive);
            ClearScreenCommand = ReactiveCommand.Create(ClearScreen);
            DeleteLastCommand = ReactiveCommand.Create(DeleteLast);
            PickResultCommand = ReactiveCommand.Create(PickResult);
        }

        public string ShownString
        {
            get => _shownString;
            set => this.RaiseAndSetIfChanged(ref _shownString, value);
        }

        public string ShownResult
        {
            get => _shownResult;
            set => this.RaiseAndSetIfChanged(ref _shownResult, value);
        }

        private void AddDecimalSeparator()
        {
            if (CanDecimalSeparatorBePlaced())
                ShownString += CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
        }

        private void AddNumber(int value)
        {
            ShownString += value;
            Calculate(ShownString);
        }

        private void AddOperator(Operator @operator)
        {
            if (ShownString[^1].Equals('('))
                return;
            
            if (IsLastInputAnOperator())
                ShownString = ShownString[..^1];

            ShownString += @operator switch
            {
                Operator.Add => OperatorChar.Add,
                Operator.Subtract => OperatorChar.Subtract,
                Operator.Multiply => OperatorChar.Multiply,
                Operator.Divide => OperatorChar.Divide,
                _ => throw new InvalidDataException("Operator not allowed")
            };
        }

        private void AddParenthesis()
        {
            if (ShownString.Length == 0 || IsLastInputAnOperator() || ShownString[^1].Equals('('))
            {
                ShownString += "(";
                _numberOfOpeningParentheses++;
            }
            else if (_numberOfClosingParentheses < _numberOfOpeningParentheses)
            {
                ShownString += ")";
                _numberOfClosingParentheses++;
                Calculate(ShownString);
            }
        }

        private void AlternateNegativePositive()
        {
            var indexWhereSetOrUnsetSign = SetIndexWhereToSetOrUnsetSign();

            if (ShownString.Length == 0 || ShownString[^1].Equals('('))
                ShownString += OperatorChar.Subtract;
            else
            {
                switch (ShownString[indexWhereSetOrUnsetSign])
                {
                    case OperatorChar.Subtract:
                        if (indexWhereSetOrUnsetSign == 0 ||
                            ShownString[indexWhereSetOrUnsetSign - 1].Equals('(') ||
                            OperatorChar.IsAnOperator(ShownString[indexWhereSetOrUnsetSign - 1]))
                            
                            ShownString = ShownString.Remove(indexWhereSetOrUnsetSign, 1);
                        else
                            // Add -
                            ShownString = ShownString[..indexWhereSetOrUnsetSign] +
                                          OperatorChar.Subtract +
                                          ShownString[indexWhereSetOrUnsetSign..];
                        break;
                    default:
                        // Add -
                        ShownString = ShownString[..indexWhereSetOrUnsetSign] +
                                      OperatorChar.Subtract +
                                      ShownString[indexWhereSetOrUnsetSign..];
                        break;
                }
                
                Calculate(ShownString);
            }
        }

        private void Calculate(string calc)
        {
            ShownResult = Calculator.Calculate(calc);
        }
        
        private bool CanDecimalSeparatorBePlaced()
        {
            var indexLastDecimalSeparator = ShownString.LastIndexOf(
                CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
                StringComparison.Ordinal);
            var indexLastOperator = ShownString.LastIndexOfAny(OperatorChar.Operators);

            if (indexLastDecimalSeparator == -1 && indexLastOperator == -1)
                return true;
            
            return indexLastOperator > indexLastDecimalSeparator;
        }

        private void ClearScreen()
        {
            ShownString = string.Empty;
            ShownResult = string.Empty;
            _numberOfOpeningParentheses = 0;
            _numberOfClosingParentheses = 0;
        }

        private void DeleteLast()
        {
            if (ShownString.Length == 1)
            {
                ClearScreen();
                return;
            }

            // Update number of parentheses
            switch (ShownString[^1])
            {
                case '(':
                    _numberOfOpeningParentheses--;
                    break;
                case ')':
                    _numberOfClosingParentheses--;
                    break;
            }
            
            ShownString = ShownString[..^1];
            Calculate(IsLastInputAnOperator() ? ShownString[..^1] : ShownString);
        }

        private bool IsLastInputAnOperator()
        {
            return OperatorChar.IsAnOperator(ShownString[^1]);
        }
        
        private static int MaxOf(int number1, int number2)
        {
            return number1 > number2 ? number1 : number2;
        }
        
        private void PickResult()
        {
            ShownString = ShownResult;
            ShownResult = string.Empty;
        }

        private int SetIndexWhereToSetOrUnsetSign()
        {
            char[] nonSubstractOperators = { OperatorChar.Add, OperatorChar.Multiply, OperatorChar.Divide };
            
            var indexAfterLastNonSubstractOperator = ShownString.LastIndexOfAny(nonSubstractOperators) + 1;
            var indexOfLastSubstractOperator = ShownString.LastIndexOf(OperatorChar.Subtract);
            var indexAfterLastParenthesis = ShownString.LastIndexOf('(') + 1;
            var indexLastOperator = MaxOf(indexAfterLastNonSubstractOperator, indexOfLastSubstractOperator);

            return MaxOf(indexAfterLastParenthesis, indexLastOperator);
        }
    }
}