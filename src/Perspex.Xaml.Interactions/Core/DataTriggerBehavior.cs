// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Perspex.Xaml.Interactions.Core
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using Interactivity;

    /// <summary>
    /// A behavior that performs actions when the bound data meets a specified condition.
    /// </summary>
    /// TODO:
    ///[ContentPropertyAttribute(Name = "Actions")]
    public sealed class DataTriggerBehavior : PerspexObject, IBehavior
    {
        static DataTriggerBehavior()
        {
            BindingProperty.Changed.Subscribe(e => OnValueChanged(e.Sender, e));
            ComparisonConditionProperty.Changed.Subscribe(e => OnValueChanged(e.Sender, e));
            ValueProperty.Changed.Subscribe(e => OnValueChanged(e.Sender, e));
        }

        /// <summary>
        /// Identifies the <seealso cref="Actions"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty ActionsProperty = 
            PerspexProperty.Register<DataTriggerBehavior, ActionCollection>("Actions");

        /// <summary>
        /// Identifies the <seealso cref="Binding"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty BindingProperty = 
            PerspexProperty.Register<DataTriggerBehavior, object>("Binding");

        /// <summary>
        /// Identifies the <seealso cref="ComparisonCondition"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty ComparisonConditionProperty =
            PerspexProperty.Register<DataTriggerBehavior, ComparisonConditionType>("ComparisonCondition", ComparisonConditionType.Equal);

        /// <summary>
        /// Identifies the <seealso cref="Value"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty ValueProperty =
            PerspexProperty.Register<DataTriggerBehavior, object>("Value");

        private PerspexObject associatedObject;

        /// <summary>
        /// Gets the collection of actions associated with the behavior. This is a dependency property.
        /// </summary>
        public ActionCollection Actions
        {
            get
            {
                ActionCollection actionCollection = (ActionCollection)this.GetValue(DataTriggerBehavior.ActionsProperty);
                if (actionCollection == null)
                {
                    actionCollection = new ActionCollection();
                    this.SetValue(DataTriggerBehavior.ActionsProperty, actionCollection);
                }

                return actionCollection;
            }
        }

        /// <summary>
        /// Gets or sets the bound object that the <see cref="DataTriggerBehavior"/> will listen to. This is a dependency property.
        /// </summary>
        public object Binding
        {
            get
            {
                return (object)this.GetValue(DataTriggerBehavior.BindingProperty);
            }
            set
            {
                this.SetValue(DataTriggerBehavior.BindingProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the type of comparison to be performed between <see cref="DataTriggerBehavior.Binding"/> and <see cref="DataTriggerBehavior.Value"/>. This is a dependency property.
        /// </summary>
        public ComparisonConditionType ComparisonCondition
        {
            get
            {
                return (ComparisonConditionType)this.GetValue(DataTriggerBehavior.ComparisonConditionProperty);
            }
            set
            {
                this.SetValue(DataTriggerBehavior.ComparisonConditionProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the value to be compared with the value of <see cref="DataTriggerBehavior.Binding"/>. This is a dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1721:PropertyNamesShouldNotMatchGetMethods")]
        public object Value
        {
            get
            {
                return (object)this.GetValue(DataTriggerBehavior.ValueProperty);
            }
            set
            {
                this.SetValue(DataTriggerBehavior.ValueProperty, value);
            }
        }

        /// <summary>
        /// Gets the <seealso cref="PerspexObject"/> to which the <seealso cref="IBehavior"/> is attached.
        /// </summary>
        public PerspexObject AssociatedObject
        {
            get
            {
                return this.associatedObject;
            }
        }

        /// <summary>
        /// Attaches to the specified object.
        /// </summary>
        /// <param name="associatedObject">The <seealso cref="PerspexObject"/> to which the <seealso cref="IBehavior"/> will be attached.</param>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1500:VariableNamesShouldNotMatchFieldNames", MessageId = "associatedObject")]
        public void Attach(PerspexObject associatedObject)
        {
            // TODO: Check for design mode
            if (associatedObject == this.associatedObject /*|| Windows.ApplicationModel.DesignMode.DesignModeEnabled*/)
            {
                return;
            }

            if (this.associatedObject != null)
            {
                throw new InvalidOperationException(string.Format(
                    CultureInfo.CurrentCulture,
                    // TODO: Replace string from original resources
                    "CannotAttachBehaviorMultipleTimesExceptionMessage",
                    associatedObject,
                    this.associatedObject));
            }

            Debug.Assert(associatedObject != null, "Cannot attach the behavior to a null object.");

            this.associatedObject = associatedObject;
        }

        /// <summary>
        /// Detaches this instance from its associated object.
        /// </summary>
        public void Detach()
        {
            this.associatedObject = null;
        }

        private static bool Compare(object leftOperand, ComparisonConditionType operatorType, object rightOperand)
        {
            if (leftOperand != null && rightOperand != null)
            {
                rightOperand = TypeConverterHelper.Convert(rightOperand.ToString(), leftOperand.GetType().FullName);
            }

            IComparable leftComparableOperand = leftOperand as IComparable;
            IComparable rightComparableOperand = rightOperand as IComparable;
            if ((leftComparableOperand != null) && (rightComparableOperand != null))
            {
                return DataTriggerBehavior.EvaluateComparable(leftComparableOperand, operatorType, rightComparableOperand);
            }

            switch (operatorType)
            {
                case ComparisonConditionType.Equal:
                    return object.Equals(leftOperand, rightOperand);

                case ComparisonConditionType.NotEqual:
                    return !object.Equals(leftOperand, rightOperand);

                case ComparisonConditionType.LessThan:
                case ComparisonConditionType.LessThanOrEqual:
                case ComparisonConditionType.GreaterThan:
                case ComparisonConditionType.GreaterThanOrEqual:
                    {
                        if (leftComparableOperand == null && rightComparableOperand == null)
                        {
                            throw new ArgumentException(string.Format(
                                CultureInfo.CurrentCulture,
                                // TODO: Replace string from original resources
                                "InvalidOperands",
                                leftOperand != null ? leftOperand.GetType().Name : "null",
                                rightOperand != null ? rightOperand.GetType().Name : "null",
                                operatorType.ToString()));
                        }
                        else if (leftComparableOperand == null)
                        {
                            throw new ArgumentException(string.Format(
                                CultureInfo.CurrentCulture,
                                // TODO: Replace string from original resources
                                "InvalidLeftOperand",
                                leftOperand != null ? leftOperand.GetType().Name : "null",
                                operatorType.ToString()));
                        }
                        else
                        {
                            throw new ArgumentException(string.Format(
                                CultureInfo.CurrentCulture,
                                // TODO: Replace string from original resources
                                "InvalidRightOperand",
                                rightOperand != null ? rightOperand.GetType().Name : "null",
                                operatorType.ToString()));
                        }
                    }
            }

            return false;
        }

        /// <summary>
        /// Evaluates both operands that implement the IComparable interface.
        /// </summary>
        private static bool EvaluateComparable(IComparable leftOperand, ComparisonConditionType operatorType, IComparable rightOperand)
        {
            object convertedOperand = null;
            try
            {
                convertedOperand = Convert.ChangeType(rightOperand, leftOperand.GetType(), CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                // FormatException: Convert.ChangeType("hello", typeof(double), ...);
            }
            catch (InvalidCastException)
            {
                // InvalidCastException: Convert.ChangeType(4.0d, typeof(Rectangle), ...);
            }

            if (convertedOperand == null)
            {
                return operatorType == ComparisonConditionType.NotEqual;
            }

            int comparison = leftOperand.CompareTo((IComparable)convertedOperand);
            switch (operatorType)
            {
                case ComparisonConditionType.Equal:
                    return comparison == 0;

                case ComparisonConditionType.NotEqual:
                    return comparison != 0;

                case ComparisonConditionType.LessThan:
                    return comparison < 0;

                case ComparisonConditionType.LessThanOrEqual:
                    return comparison <= 0;

                case ComparisonConditionType.GreaterThan:
                    return comparison > 0;

                case ComparisonConditionType.GreaterThanOrEqual:
                    return comparison >= 0;
            }

            return false;
        }

        private static void OnValueChanged(PerspexObject dependencyObject, PerspexPropertyChangedEventArgs args)
        {
            DataTriggerBehavior dataTriggerBehavior = (DataTriggerBehavior)dependencyObject;
            if (dataTriggerBehavior.AssociatedObject == null)
            {
                return;
            }

            DataBindingHelper.RefreshDataBindingsOnActions(dataTriggerBehavior.Actions);

            // Some value has changed--either the binding value, reference value, or the comparison condition. Re-evaluate the equation.
            if (DataTriggerBehavior.Compare(dataTriggerBehavior.Binding, dataTriggerBehavior.ComparisonCondition, dataTriggerBehavior.Value))
            {
                Interaction.ExecuteActions(dataTriggerBehavior.AssociatedObject, dataTriggerBehavior.Actions, args);
            }
        }
    }
}
