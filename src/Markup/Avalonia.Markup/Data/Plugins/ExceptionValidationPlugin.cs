// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Data;
using System;
using System.Reflection;

namespace Avalonia.Markup.Data.Plugins
{
    /// <summary>
    /// Validates properties that report errors by throwing exceptions.
    /// </summary>
    public class ExceptionValidationPlugin : IDataValidationPlugin
    {
        /// <inheritdoc/>
        public bool Match(WeakReference reference, string memberName) => true;

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference reference, string name, IPropertyAccessor inner)
        {
            return new Validator(reference, name, inner);
        }

        private class Validator : DataValidatiorBase
        {
            public Validator(WeakReference reference, string name, IPropertyAccessor inner)
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
                    Observer.OnNext(new BindingNotification(ex.InnerException, BindingErrorType.DataValidationError));
                }
                catch (Exception ex)
                {
                    Observer.OnNext(new BindingNotification(ex, BindingErrorType.DataValidationError));
                }

                return false;
            }
        }
    }
}
