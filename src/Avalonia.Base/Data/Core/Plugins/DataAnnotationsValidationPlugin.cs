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
        public bool Match(WeakReference<object> reference, string memberName)
        {
            reference.TryGetTarget(out object target);

            return target?
                .GetType()
                .GetRuntimeProperty(memberName)?
                .GetCustomAttributes<ValidationAttribute>()
                .Any() ?? false;
        }

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference<object> reference, string name, IPropertyAccessor inner)
        {
            return new Accessor(reference, name, inner);
        }

        private sealed class Accessor : DataValidationBase
        {
            private readonly ValidationContext _context;

            public Accessor(WeakReference<object> reference, string name, IPropertyAccessor inner)
                : base(inner)
            {
                reference.TryGetTarget(out object target);

                _context = new ValidationContext(target);
                _context.MemberName = name;
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
