// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls.Templates;
using Avalonia.Data;

namespace Avalonia.Controls
{
    /// <summary>
    /// A control which displays an error notifier when there is a DataValidationError. 
    /// Provides attached properties to track errors on a control
    /// </summary>
    /// <remarks>
    /// You will probably only want to create instances inside of control templates.
    /// </remarks>
    public class DataValidationErrors : ContentControl
    {
        /// <summary>
        /// Defines the DataValidationErrors.Errors attached property.
        /// </summary>
        public static readonly AttachedProperty<IEnumerable<object>> ErrorsProperty =
            AvaloniaProperty.RegisterAttached<DataValidationErrors, Control, IEnumerable<object>>("Errors");

        /// <summary>
        /// Defines the DataValidationErrors.HasErrors attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> HasErrorsProperty =
            AvaloniaProperty.RegisterAttached<DataValidationErrors, Control, bool>("HasErrors");

        public static readonly StyledProperty<IDataTemplate> ErrorTemplateProperty =
            AvaloniaProperty.Register<DataValidationErrors, IDataTemplate>(nameof(ErrorTemplate));


        private Control _owner;

        public static readonly DirectProperty<DataValidationErrors, Control> OwnerProperty =
            AvaloniaProperty.RegisterDirect<DataValidationErrors, Control>(
                nameof(Owner),
                o => o.Owner,
                (o, v) => o.Owner = v);

        public Control Owner
        {
            get { return _owner; }
            set { SetAndRaise(OwnerProperty, ref _owner, value); }
        }

        /// <summary>
        /// Initializes static members of the <see cref="DataValidationErrors"/> class.
        /// </summary>
        static DataValidationErrors()
        {
            ErrorsProperty.Changed.Subscribe(ErrorsChanged);
            HasErrorsProperty.Changed.Subscribe(HasErrorsChanged);
            TemplatedParentProperty.Changed.AddClassHandler<DataValidationErrors>((x, e) => x.OnTemplatedParentChange(e));
        }

        private void OnTemplatedParentChange(AvaloniaPropertyChangedEventArgs e)
        {
            if (Owner == null)
            {
                Owner = (e.NewValue as Control);
            }
        }

        public IDataTemplate ErrorTemplate
        {
            get { return GetValue(ErrorTemplateProperty); }
            set { SetValue(ErrorTemplateProperty, value); }
        }

        private static void ErrorsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;
            var errors = (IEnumerable<object>)e.NewValue;

            var hasErrors = false;
            if (errors != null && errors.Any())
                hasErrors = true;

            control.SetValue(HasErrorsProperty, hasErrors);
        }
        private static void HasErrorsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;
            var classes = (IPseudoClasses)control.Classes;
            classes.Set(":error", (bool)e.NewValue);
        }

        public static IEnumerable<object> GetErrors(Control control)
        {
            return control.GetValue(ErrorsProperty);
        }
        public static void SetErrors(Control control, IEnumerable<object> errors)
        {
            control.SetValue(ErrorsProperty, errors);
        }
        public static void SetError(Control control, Exception error)
        {
            SetErrors(control, UnpackException(error));
        }
        public static void ClearErrors(Control control)
        {
            SetErrors(control, null);
        }
        public static bool GetHasErrors(Control control)
        {
            return control.GetValue(HasErrorsProperty);
        }

        private static IEnumerable<object> UnpackException(Exception exception)
        {
            if (exception != null)
            {
                var aggregate = exception as AggregateException;
                var exceptions = aggregate == null ?
                    new[] { GetExceptionData(exception) } :
                    aggregate.InnerExceptions.Select(GetExceptionData).ToArray();
                var filtered = exceptions.Where(x => !(x is BindingChainException)).ToList();

                if (filtered.Count > 0)
                {
                    return filtered;
                }
            }

            return null;
        }

        private static object GetExceptionData(Exception exception)
        {
            if (exception is DataValidationException dataValidationException)
                return dataValidationException.ErrorData;

            return exception;
        }
    }
}
