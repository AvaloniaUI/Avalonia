// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Perspex.Xaml.Interactions.Core
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices.WindowsRuntime;
    using Interactivity;
    using Controls;

    /// <summary>
    /// A behavior that listens for a specified event on its source and executes its actions when that event is fired.
    /// </summary>
    /// TODO:
    ///[ContentPropertyAttribute(Name = "Actions")]
    public sealed class EventTriggerBehavior : PerspexObject, IBehavior
    {
        /// <summary>
        /// Identifies the <seealso cref="Actions"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty ActionsProperty = PerspexProperty.Register<EventTriggerBehavior, ActionCollection>(
            "Actions");
            // TODO: new PropertyMetadata(null));

        /// <summary>
        /// Identifies the <seealso cref="EventName"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty EventNameProperty = PerspexProperty.Register<EventTriggerBehavior, string>(
            "EventName");
            // TODO: new PropertyMetadata("Loaded", new PropertyChangedCallback(EventTriggerBehavior.OnEventNameChanged)));

        /// <summary>
        /// Identifies the <seealso cref="SourceObject"/> dependency property.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty SourceObjectProperty = PerspexProperty.Register<EventTriggerBehavior, object>(
            "SourceObject");
        // TODO: new PropertyMetadata(null, new PropertyChangedCallback(EventTriggerBehavior.OnSourceObjectChanged)));

        private PerspexObject associatedObject;
        private object resolvedSource;
        private Delegate eventHandler;
        private bool isLoadedEventRegistered;
        private bool isWindowsRuntimeEvent;

        /// <summary>
        /// Initializes a new instance of the <see cref="EventTriggerBehavior"/> class.
        /// </summary>
        public EventTriggerBehavior()
        {
        }

        /// <summary>
        /// Gets the collection of actions associated with the behavior. This is a dependency property.
        /// </summary>
        public ActionCollection Actions
        {
            get
            {
                ActionCollection actionCollection = (ActionCollection)this.GetValue(EventTriggerBehavior.ActionsProperty);
                if (actionCollection == null)
                {
                    actionCollection = new ActionCollection();
                    this.SetValue(EventTriggerBehavior.ActionsProperty, actionCollection);
                }

                return actionCollection;
            }
        }

        /// <summary>
        /// Gets or sets the name of the event to listen for. This is a dependency property.
        /// </summary>
        public string EventName
        {
            get
            {
                return (string)this.GetValue(EventTriggerBehavior.EventNameProperty);
            }

            set
            {
                this.SetValue(EventTriggerBehavior.EventNameProperty, value);
            }
        }

        /// <summary>
        /// Gets or sets the source object from which this behavior listens for events.
        /// If <seealso cref="SourceObject"/> is not set, the source will default to <seealso cref="AssociatedObject"/>. This is a dependency property.
        /// </summary>
        public object SourceObject
        {
            get
            {
                return (object)this.GetValue(EventTriggerBehavior.SourceObjectProperty);
            }

            set
            {
                this.SetValue(EventTriggerBehavior.SourceObjectProperty, value);
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
            this.SetResolvedSource(this.ComputeResolvedSource());
        }

        /// <summary>
        /// Detaches this instance from its associated object.
        /// </summary>
        public void Detach()
        {
            this.SetResolvedSource(null);
            this.associatedObject = null;
        }

        private void SetResolvedSource(object newSource)
        {
            if (this.AssociatedObject == null || this.resolvedSource == newSource)
            {
                return;
            }

            if (this.resolvedSource != null)
            {
                this.UnregisterEvent(this.EventName);
            }

            this.resolvedSource = newSource;

            if (this.resolvedSource != null)
            {
                this.RegisterEvent(this.EventName);
            }
        }

        private object ComputeResolvedSource()
        {
            // If the SourceObject property is set at all, we want to use it. It is possible that it is data
            // bound and bindings haven't been evaluated yet. Plus, this makes the API more predictable.
            // TODO: use this.ReadLocalValue
            if (this.GetValue(EventTriggerBehavior.SourceObjectProperty) != PerspexProperty.UnsetValue)
            {
                return this.SourceObject;
            }

            return this.AssociatedObject;
        }

        private void RegisterEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (eventName != "Loaded")
            {
                Type sourceObjectType = this.resolvedSource.GetType();
                EventInfo info = sourceObjectType.GetRuntimeEvent(this.EventName);
                if (info == null)
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        // TODO: Replace string from original resources
                        "CannotFindEventNameExceptionMessage",
                        this.EventName,
                        sourceObjectType.Name));
                }

                MethodInfo methodInfo = typeof(EventTriggerBehavior).GetTypeInfo().GetDeclaredMethod("OnEvent");
                this.eventHandler = methodInfo.CreateDelegate(info.EventHandlerType, this);

                this.isWindowsRuntimeEvent = EventTriggerBehavior.IsWindowsRuntimeType(info.EventHandlerType);
                if (this.isWindowsRuntimeEvent)
                {
                    WindowsRuntimeMarshal.AddEventHandler<Delegate>(
                                add => (EventRegistrationToken)info.AddMethod.Invoke(this.resolvedSource, new object[] { add }),
                                token => info.RemoveMethod.Invoke(this.resolvedSource, new object[] { token }),
                                this.eventHandler);
                }
                else
                {
                    info.AddEventHandler(this.resolvedSource, this.eventHandler);
                }
            }
            else if (!this.isLoadedEventRegistered)
            {
                Control element = this.resolvedSource as Control;
                if (element != null && !EventTriggerBehavior.IsElementLoaded(element))
                {
                    this.isLoadedEventRegistered = true;
                    element.AttachedToVisualTree += this.OnEvent;
                }
            }
        }

        private void UnregisterEvent(string eventName)
        {
            if (string.IsNullOrEmpty(eventName))
            {
                return;
            }

            if (eventName != "Loaded")
            {
                if (this.eventHandler == null)
                {
                    return;
                }

                EventInfo info = this.resolvedSource.GetType().GetRuntimeEvent(eventName);
                if (this.isWindowsRuntimeEvent)
                {
                    WindowsRuntimeMarshal.RemoveEventHandler<Delegate>(
                        token => info.RemoveMethod.Invoke(this.resolvedSource, new object[] { token }),
                        this.eventHandler);
                }
                else
                {
                    info.RemoveEventHandler(this.resolvedSource, this.eventHandler);
                }

                this.eventHandler = null;
            }
            else if (this.isLoadedEventRegistered)
            {
                this.isLoadedEventRegistered = false;
                Control element = (Control)this.resolvedSource;
                element.AttachedToVisualTree -= this.OnEvent;
            }
        }

        private void OnEvent(object sender, object eventArgs)
        {
            Interaction.ExecuteActions(this.resolvedSource, this.Actions, eventArgs);
        }

        private static void OnSourceObjectChanged(PerspexObject dependencyObject, PerspexPropertyChangedEventArgs args)
        {
            EventTriggerBehavior behavior = (EventTriggerBehavior)dependencyObject;
            behavior.SetResolvedSource(behavior.ComputeResolvedSource());
        }

        private static void OnEventNameChanged(PerspexObject dependencyObject, PerspexPropertyChangedEventArgs args)
        {
            EventTriggerBehavior behavior = (EventTriggerBehavior)dependencyObject;
            if (behavior.AssociatedObject == null || behavior.resolvedSource == null)
            {
                return;
            }

            string oldEventName = (string)args.OldValue;
            string newEventName = (string)args.NewValue;

            behavior.UnregisterEvent(oldEventName);
            behavior.RegisterEvent(newEventName);
        }

        internal static bool IsElementLoaded(Control element)
        {
            if (element == null)
            {
                return false;
            }

            // TODO:
            //Control rootVisual = Window.Current.Content;
            var parent = element.Parent;
            /*
            if (parent == null)
            {
                // If the element is the child of a ControlTemplate it will have a null parent even when it is loaded.
                // To catch that scenario, also check it's parent in the visual tree.
                parent = VisualTreeHelper.GetParent(element);
            }
            */
            return (parent != null /*|| (rootVisual != null && element == rootVisual)*/);
        }

        private static bool IsWindowsRuntimeType(Type type)
        {
            if (type != null)
            {
                return type.AssemblyQualifiedName.EndsWith("ContentType=WindowsRuntime", StringComparison.Ordinal);
            }

            return false;
        }
    }
}
