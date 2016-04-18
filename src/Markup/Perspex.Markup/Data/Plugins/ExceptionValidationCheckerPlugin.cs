using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Data;

namespace Perspex.Markup.Data.Plugins
{
    /// <summary>
    /// Validates properties that report errors by throwing exceptions.
    /// </summary>
    public class ExceptionValidationCheckerPlugin : IValidationCheckerPlugin
    {

        /// <inheritdoc/>
        public bool Match(WeakReference reference) => true;


        /// <inheritdoc/>
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

        /// <summary>
        /// Describes the current validation status after setting a property value.
        /// </summary>
        public class ExceptionValidationStatus : ValidationStatus
        {
            internal ExceptionValidationStatus(Exception exception)
            {
                Exception = exception;
            }

            /// <summary>
            /// The thrown exception. If there was no thrown exception, null.
            /// </summary>
            public Exception Exception { get; }


            /// <inheritdoc/>
            public override bool IsValid => Exception == null;

            public override bool Match(ValidationMethods enabledMethods)
            {
                return (enabledMethods & ValidationMethods.Exceptions) != 0;
            }
        }
    }
}
