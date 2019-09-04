// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Validates properties that report errors by throwing exceptions.
    /// </summary>
    public class ExceptionValidationPlugin : IDataValidationPlugin
    {
        /// <inheritdoc/>
        public bool Match(WeakReference<object> reference, string memberName) => true;

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference<object> reference, string name, IPropertyAccessor inner)
        {
            return new Validator(reference, name, inner);
        }

        private sealed class Validator : DataValidationBase
        {
            public Validator(WeakReference<object> reference, string name, IPropertyAccessor inner)
                : base(inner)
            {
            }

            public override bool SetValue(object value, BindingPriority priority)
            {
                try
                {
                    return base.SetValue(value, priority);
                }
                catch (TargetInvocationException ex)
                {
                    PublishValue(new BindingNotification(ex.InnerException, BindingErrorType.DataValidationError));
                }
                catch (Exception ex)
                {
                    PublishValue(new BindingNotification(ex, BindingErrorType.DataValidationError));
                }

                return false;
            }
        }
    }
}
