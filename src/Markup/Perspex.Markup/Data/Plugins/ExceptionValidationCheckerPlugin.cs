using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Data;

namespace Perspex.Markup.Data.Plugins
{
    public class ExceptionValidationCheckerPlugin : IValidationCheckerPlugin
    {
        public ValidationCheckerBase Start(WeakReference reference, string name, IPropertyAccessor accessor, Action<ValidationStatus> callback)
        {
            return new ExceptionValidationChecker(reference, name, accessor, callback);
        }

        private class ExceptionValidationChecker : ValidationCheckerBase
        {
            public ExceptionValidationChecker(WeakReference reference, string name, IPropertyAccessor accessor, Action<ValidationStatus> callback)
                : base(reference, name, accessor, callback)
            {
            }

            public override bool SetValue(object value, BindingPriority priority)
            {
                try
                {
                    var success = base.SetValue(value, priority);
                    SendValidationCallback(new ExceptionValidationStatus(null));
                    return success;
                }
                catch (Exception ex)
                {
                    SendValidationCallback(new ExceptionValidationStatus(ex));
                }
                return false;
            }
        }

        private class ExceptionValidationStatus : ValidationStatus
        {
            public ExceptionValidationStatus(Exception exception)
            {
                Exception = exception;
            }

            public Exception Exception { get; }

            public override bool IsValid => Exception != null;
        }
    }
}
