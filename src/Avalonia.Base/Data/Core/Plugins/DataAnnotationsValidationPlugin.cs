// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Validates properties on that have <see cref="ValidationAttribute"/>s.
    /// </summary>
    public class DataAnnotationsValidationPlugin : IDataValidationPlugin
    {
        /// <inheritdoc/>
        public bool Match(WeakReference reference, string memberName)
        {
            return reference.Target?
                .GetType()
                .GetRuntimeProperty(memberName)?
                .GetCustomAttributes<ValidationAttribute>()
                .Any() ?? false;
        }

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference reference, string name, IPropertyAccessor inner)
        {
            return new Accessor(reference, name, inner);
        }

        private class Accessor : DataValidationBase
        {
            private ValidationContext _context;

            public Accessor(WeakReference reference, string name, IPropertyAccessor inner)
                : base(inner)
            {
                _context = new ValidationContext(reference.Target);
                _context.MemberName = name;
            }

            public override bool SetValue(object value, BindingPriority priority)
            {
                return base.SetValue(value, priority);
            }

            protected override void InnerValueChanged(object value)
            {
                var errors = new List<ValidationResult>();

                if (Validator.TryValidateProperty(value, _context, errors))
                {
                    base.InnerValueChanged(value);
                }
                else
                {
                    base.InnerValueChanged(new BindingNotification(
                        CreateException(errors),
                        BindingErrorType.DataValidationError,
                        value));
                }
            }

            private Exception CreateException(IList<ValidationResult> errors)
            {
                if (errors.Count == 1)
                {
                    return new ValidationException(errors[0].ErrorMessage);
                }
                else
                {
                    return new AggregateException(
                        errors.Select(x => new ValidationException(x.ErrorMessage)));
                }
            }
        }
    }
}
