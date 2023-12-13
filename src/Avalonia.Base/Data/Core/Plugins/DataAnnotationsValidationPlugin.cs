using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Avalonia.Data.Core.Plugins
{
    /// <summary>
    /// Validates properties on that have <see cref="ValidationAttribute"/>s.
    /// </summary>
    [RequiresUnreferencedCode(TrimmingMessages.DataValidationPluginRequiresUnreferencedCodeMessage)]
    public class DataAnnotationsValidationPlugin : IDataValidationPlugin
    {
        /// <inheritdoc/>
        public bool Match(WeakReference<object?> reference, string memberName)
        {
            reference.TryGetTarget(out var target);

            return target?
                .GetType()
                .GetRuntimeProperty(memberName)?
                .GetCustomAttributes<ValidationAttribute>()
                .Any() ?? false;
        }

        /// <inheritdoc/>
        public IPropertyAccessor Start(WeakReference<object?> reference, string name, IPropertyAccessor inner)
        {
            return new Accessor(reference, name, inner);
        }

        [RequiresUnreferencedCode(TrimmingMessages.DataValidationPluginRequiresUnreferencedCodeMessage)]
        private sealed class Accessor : DataValidationBase
        {
            private readonly ValidationContext? _context;

            public Accessor(WeakReference<object?> reference, string name, IPropertyAccessor inner)
                : base(inner)
            {
                if (reference.TryGetTarget(out var target))
                {
                    _context = new ValidationContext(target);
                    _context.MemberName = name;
                }
            }

            protected override void InnerValueChanged(object? value)
            {
                if (_context is null)
                    return;

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

            private static Exception CreateException(IList<ValidationResult> errors)
            {
                if (errors.Count == 1)
                {
                    return new DataValidationException(errors[0].ErrorMessage);
                }
                else
                {
                    return new AggregateException(
                        errors.Select(x => new DataValidationException(x.ErrorMessage)));
                }
            }
        }
    }
}
