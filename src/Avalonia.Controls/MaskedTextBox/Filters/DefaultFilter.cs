using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Avalonia.Controls.MaskedTextBox.Filters
{
    public class DefaultFilter
    {
        private Func<string, bool> _isTextValidCheckers;

        public static readonly DefaultFilter IntegerFilter = new(item => int.TryParse(item, out _));
        public static readonly DefaultFilter NullFilter = new();

        public DefaultFilter() { }

        public DefaultFilter(Func<string, bool> additionalValidator)
        {
            AddTextValidator(additionalValidator);
        }

        protected void AddTextValidator(Func<string, bool> additionalValidator)
        {
            if (_isTextValidCheckers is null || !_isTextValidCheckers.GetInvocationList().Contains(additionalValidator))
            {
                _isTextValidCheckers += additionalValidator;
            }
        }

        public bool IsTextValid(string newText)
        {
            if (_isTextValidCheckers is null)
            {
                return true;
            }

            foreach (var isTextValidChecker in _isTextValidCheckers.GetInvocationList())
            {
                if (isTextValidChecker is not Func<string, bool> checker)
                {
                    continue;
                }
                if (!checker(newText))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
