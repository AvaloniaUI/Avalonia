// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Perspex.Xaml.Interactions.Core
{
    using System.Windows.Input;
    using Interactivity;
    using Markup;
    /// <summary>
    /// Executes a specified <see cref="System.Windows.Input.ICommand"/> when invoked. 
    /// </summary>
    public sealed class InvokeCommandAction : PerspexObject, IAction
    {
        /// <summary>
        /// Identifies the <seealso cref="Command"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty CommandProperty = PerspexProperty.Register<InvokeCommandAction, ICommand>(
            "Command");
            // TODO: new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <seealso cref="CommandParameter"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty CommandParameterProperty = PerspexProperty.Register<InvokeCommandAction, object>(
            "CommandParameter");
            // TODO: new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <seealso cref="InputConverter"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty InputConverterProperty = PerspexProperty.Register<InvokeCommandAction, IValueConverter>(
            "InputConverter");
            // TODO: new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <seealso cref="InputConverterParameter"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty InputConverterParameterProperty = PerspexProperty.Register<InvokeCommandAction, object>(
            "InputConverterParameter");
            // TODO: new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <seealso cref="InputConverterLanguage"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty InputConverterLanguageProperty = PerspexProperty.Register<InvokeCommandAction, string>(
            "InputConverterLanguage");
            // TODO: new PropertyMetadata(string.Empty)); // Empty string means the invariant culture.

        /// <summary>
        /// Gets or sets the command this action should invoke. This is a dependency property.
        /// </summary>
        public ICommand Command
        {
            get
            {
                return (ICommand)this.GetValue(InvokeCommandAction.CommandProperty);
            }
            set
            {
                this.SetValue(InvokeCommandAction.CommandProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the parameter that is passed to <see cref="System.Windows.Input.ICommand.Execute(object)"/>.
        /// If this is not set, the parameter from the <seealso cref="Execute(object, object)"/> method will be used.
        /// This is an optional dependency property.
        /// </summary>
        public object CommandParameter
        {
            get
            {
                return this.GetValue(InvokeCommandAction.CommandParameterProperty);
            }
            set
            {
                this.SetValue(InvokeCommandAction.CommandParameterProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the converter that is run on the parameter from the <seealso cref="Execute(object, object)"/> method.
        /// This is an optional dependency property.
        /// </summary>
        public IValueConverter InputConverter
        {
            get
            {
                return (IValueConverter)this.GetValue(InvokeCommandAction.InputConverterProperty);
            }
            set
            {
                this.SetValue(InvokeCommandAction.InputConverterProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the parameter that is passed to the <see cref="Windows.UI.Xaml.Data.IValueConverter.Convert"/>
        /// method of <see cref="InputConverter"/>.
        /// This is an optional dependency property.
        /// </summary>
        public object InputConverterParameter
        {
            get
            {
                return this.GetValue(InvokeCommandAction.InputConverterParameterProperty);
            }
            set
            {
                this.SetValue(InvokeCommandAction.InputConverterParameterProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the language that is passed to the <see cref="Windows.UI.Xaml.Data.IValueConverter.Convert"/>
        /// method of <see cref="InputConverter"/>.
        /// This is an optional dependency property.
        /// </summary>
        public string InputConverterLanguage
        {
            get
            {
                return (string)this.GetValue(InvokeCommandAction.InputConverterLanguageProperty);
            }
            set
            {
                this.SetValue(InvokeCommandAction.InputConverterLanguageProperty, value);
            }
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="sender">The <see cref="System.Object"/> that is passed to the action by the behavior. Generally this is <seealso cref="Microsoft.Xaml.Interactivity.IBehavior.AssociatedObject"/> or a target object.</param>
        /// <param name="parameter">The value of this parameter is determined by the caller.</param>
        /// <returns>True if the command is successfully executed; else false.</returns>
        public object Execute(object sender, object parameter)
        {
            if (this.Command == null)
            {
                return false;
            }

            object resolvedParameter;
            if (this.CommandParameter != null)
            {
                resolvedParameter = this.CommandParameter;
            }
            else if (this.InputConverter != null)
            {
                resolvedParameter = this.InputConverter.Convert(
                    parameter,
                    typeof(object),
                    this.InputConverterParameter,
                    new System.Globalization.CultureInfo(this.InputConverterLanguage)); // TODO:
            }
            else
            {
                resolvedParameter = parameter;
            }

            if (!this.Command.CanExecute(resolvedParameter))
            {
                return false;
            }

            this.Command.Execute(resolvedParameter);
            return true;
        }
    }
}
