using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Reactive;
using Avalonia.Controls.Metadata;
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
    [PseudoClasses(":error")]
    public class DataValidationErrors : ContentControl
    {
        /// <summary>
        /// Defines the DataValidationErrors.Errors attached property.
        /// </summary>
        public static readonly AttachedProperty<IEnumerable<object>?> ErrorsProperty =
            AvaloniaProperty.RegisterAttached<DataValidationErrors, Control, IEnumerable<object>?>("Errors");

        /// <summary>
        /// Defines the DataValidationErrors.HasErrors attached property.
        /// </summary>
        public static readonly AttachedProperty<bool> HasErrorsProperty =
            AvaloniaProperty.RegisterAttached<DataValidationErrors, Control, bool>("HasErrors");
        
        /// <summary>
        /// Defines the DataValidationErrors.ErrorConverter attached property.
        /// </summary>
        public static readonly AttachedProperty<Func<object, object>?> ErrorConverterProperty =
            AvaloniaProperty.RegisterAttached<DataValidationErrors, Control, Func<object, object>?>("ErrorConverter");

        
        public static readonly StyledProperty<IDataTemplate> ErrorTemplateProperty =
            AvaloniaProperty.Register<DataValidationErrors, IDataTemplate>(nameof(ErrorTemplate));

        /// <summary>
        /// Stores the original, not converted errors passed by the control
        /// </summary>
        private static readonly AttachedProperty<IEnumerable<object>?> OriginalErrorsProperty =
            AvaloniaProperty.RegisterAttached<DataValidationErrors, Control, IEnumerable<object>?>("OriginalErrors");

        private Control? _owner;

        public static readonly DirectProperty<DataValidationErrors, Control?> OwnerProperty =
            AvaloniaProperty.RegisterDirect<DataValidationErrors, Control?>(
                nameof(Owner),
                o => o.Owner,
                (o, v) => o.Owner = v);

        public Control? Owner
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
            ErrorConverterProperty.Changed.Subscribe(OnErrorConverterChanged);
        }

        private static void OnErrorConverterChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;
            var converter = e.NewValue as Func<object, object>;
            
            control.SetValue(ErrorsProperty, 
                control.GetValue(OriginalErrorsProperty)?.Select(err => converter is null ? err : converter.Invoke(err)));
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
            var errors = (IEnumerable<object>?)e.NewValue;

            var hasErrors = false;
            if (errors != null && errors.Any())
                hasErrors = true;

            control.SetValue(HasErrorsProperty, hasErrors);

            // Update original errors
            control.SetValue(OriginalErrorsProperty, errors);
        }
        
        private static void HasErrorsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            var control = (Control)e.Sender;
            var classes = (IPseudoClasses)control.Classes;
            classes.Set(":error", (bool)e.NewValue!);
        }

        public static IEnumerable<object>? GetErrors(Control control)
        {
            return control.GetValue(ErrorsProperty);
        }
        public static void SetErrors(Control control, IEnumerable<object>? errors)
        {
            control.SetValue(ErrorsProperty, errors);
        }
        public static void SetError(Control control, Exception? error)
        {
            SetErrors(control, UnpackException(error, control));
        }
        public static void ClearErrors(Control control)
        {
            SetErrors(control, null);
        }
        public static bool GetHasErrors(Control control)
        {
            return control.GetValue(HasErrorsProperty);
        }

        public static Func<object, object>? GetErrorConverter(Control control)
        {
            return control.GetValue(ErrorConverterProperty);
        }
        
        public static void SetErrorConverter(Control control, Func<object, object>? converter)
        {
            control.SetValue(ErrorConverterProperty, converter);
        }
        
        private static IEnumerable<object>? UnpackException(Exception? exception, Control control)
        {
            if (exception != null)
            {
                var converter = GetErrorConverter(control);
                var aggregate = exception as AggregateException;
                var exceptions = aggregate == null ?
                    new[] { GetExceptionData(exception, converter) } :
                    aggregate.InnerExceptions.Select(x => GetExceptionData(x, converter)).ToArray();
                var filtered = exceptions.Where(x => !(x is BindingChainException)).ToList();

                if (filtered.Count > 0)
                {
                    return filtered;
                }
            }

            return null;
        }

        private static object GetExceptionData(Exception exception, Func<object, object>? converter)
        {
            if (exception is DataValidationException dataValidationException &&
                dataValidationException.ErrorData is object data)
                return converter is null ? data : converter.Invoke(data);

            return converter is null ? exception : converter.Invoke(exception);
        }
    }
}
