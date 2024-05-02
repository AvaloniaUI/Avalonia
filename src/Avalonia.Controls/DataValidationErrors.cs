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
        private static bool s_overridingErrors;
        
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

        /// <summary>
        /// Defines the DataValidationErrors.ErrorTemplate property.
        /// </summary>
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
            get => _owner;
            set => SetAndRaise(OwnerProperty, ref _owner, value);
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
            OnErrorsOrConverterChanged((Control)e.Sender);
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
            get => GetValue(ErrorTemplateProperty);
            set => SetValue(ErrorTemplateProperty, value);
        }

        private static void ErrorsChanged(AvaloniaPropertyChangedEventArgs e)
        {
            if (s_overridingErrors) return;

            var control = (Control)e.Sender;
            var errors = (IEnumerable<object>?)e.NewValue;

            // Update original errors
            control.SetValue(OriginalErrorsProperty, errors);

            OnErrorsOrConverterChanged(control);
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
            SetErrors(control, UnpackException(error)?
                .Select(UnpackDataValidationException)
                .Where(e => e is not null)
                .ToArray()!);
        }

        private static void OnErrorsOrConverterChanged(Control control)
        {
            var converter = GetErrorConverter(control);
            var originalErrors = control.GetValue(OriginalErrorsProperty);
            var newErrors = (converter is null ?
                originalErrors :
                originalErrors?.Select(converter)
                    .Where(e => e is not null))?
                .ToArray();

            s_overridingErrors = true;
            try
            {
                control.SetCurrentValue(ErrorsProperty, newErrors!);
            }
            finally
            {
                s_overridingErrors = false;
            }

            control.SetValue(HasErrorsProperty, newErrors?.Any() == true);
        }

        public static void ClearErrors(Control control)
        {
            SetErrors(control, null);
        }
        public static bool GetHasErrors(Control control)
        {
            return control.GetValue(HasErrorsProperty);
        }

        public static Func<object, object?>? GetErrorConverter(Control control)
        {
            return control.GetValue(ErrorConverterProperty);
        }

        public static void SetErrorConverter(Control control, Func<object, object>? converter)
        {
            control.SetValue(ErrorConverterProperty, converter);
        }

        private static IEnumerable<Exception>? UnpackException(Exception? exception)
        {
            if (exception != null)
            {
                var exceptions = exception is AggregateException aggregate ?
                    aggregate.InnerExceptions :
                    (IEnumerable<Exception>)new[] { exception };

                return exceptions.Where(x => !(x is BindingChainException)).ToArray();
            }

            return null;
        }

        private static object? UnpackDataValidationException(Exception exception)
        {
            if (exception is DataValidationException dataValidationException)
            {
                return dataValidationException.ErrorData;
            }

            return exception;
        }
    }
}
