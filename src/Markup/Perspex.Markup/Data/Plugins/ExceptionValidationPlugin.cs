using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Perspex.Data;
using System.Reflection;

namespace Perspex.Markup.Data.Plugins
{
    /// <summary>
    /// Validates properties that report errors by throwing exceptions.
    /// </summary>
    public class ExceptionValidationPlugin : IValidationPlugin
    {

        /// <inheritdoc/>
        public bool Match(WeakReference reference) => true;


        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference reference, string name, IPropertyAccessor accessor, Action<IValidationStatus> callback)
        {
            return new ExceptionValidationChecker(reference, name, accessor, callback);
        }

        private class ExceptionValidationChecker : ValidatingPropertyAccessorBase
        {
            public ExceptionValidationChecker(WeakReference reference, string name, IPropertyAccessor accessor, Action<IValidationStatus> callback)
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
                catch (TargetInvocationException ex)
                {
                    SendValidationCallback(new ExceptionValidationStatus(ex.InnerException));
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
        public class ExceptionValidationStatus : IFilterableValidationStatus
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
            public bool IsValid => Exception == null;

            public bool Match(ValidationMethods enabledMethods)
            {
                return (enabledMethods & ValidationMethods.Exceptions) != 0;
            }
        }
    }
}
