﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using Perspex.Xaml.Interactivity;

namespace Perspex.Xaml.Interactions.Core
{
    /// <summary>
    /// An action that calls a method on a specified object when invoked.
    /// </summary>
    public sealed class CallMethodAction : PerspexObject, IAction
    {
        static CallMethodAction()
        {
            MethodNameProperty.Changed.Subscribe(e =>
            {
                CallMethodAction callMethodAction = (CallMethodAction)e.Sender;
                callMethodAction.UpdateMethodDescriptors();
            });

            TargetObjectProperty.Changed.Subscribe(e =>
            {
                CallMethodAction callMethodAction = (CallMethodAction)e.Sender;

                Type newType = e.NewValue != null ? e.NewValue.GetType() : null;
                callMethodAction.UpdateTargetType(newType);
            });
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty MethodNameProperty =
            PerspexProperty.Register<CallMethodAction, string>("MethodName");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly PerspexProperty TargetObjectProperty =
            PerspexProperty.Register<CallMethodAction, object>("TargetObject");

        private Type targetObjectType;
        private List<MethodDescriptor> methodDescriptors = new List<MethodDescriptor>();
        private MethodDescriptor cachedMethodDescriptor;

        /// <summary>
        /// Gets or sets the name of the method to invoke. This is a dependency property.
        /// </summary>
        public string MethodName
        {
            get { return (string)this.GetValue(CallMethodAction.MethodNameProperty); }
            set { this.SetValue(CallMethodAction.MethodNameProperty, value); }
        }

        /// <summary>
        /// Gets or sets the object that exposes the method of interest. This is a dependency property.
        /// </summary>
        public object TargetObject
        {
            get { return this.GetValue(CallMethodAction.TargetObjectProperty); }
            set { this.SetValue(CallMethodAction.TargetObjectProperty, value); }
        }

        /// <summary>
        /// Executes the action.
        /// </summary>
        /// <param name="sender">The <see cref="System.Object"/> that is passed to the action by the behavior. Generally this is <seealso cref="IBehavior.AssociatedObject"/> or a target object.</param>
        /// <param name="parameter">The value of this parameter is determined by the caller.</param>
        /// <returns>True if the method is called; else false.</returns>
        public object Execute(object sender, object parameter)
        {
            object target;
            if (this.GetValue(CallMethodAction.TargetObjectProperty) != PerspexProperty.UnsetValue)
            {
                target = this.TargetObject;
            }
            else
            {
                target = sender;
            }

            if (target == null || string.IsNullOrEmpty(this.MethodName))
            {
                return false;
            }

            this.UpdateTargetType(target.GetType());

            MethodDescriptor methodDescriptor = this.FindBestMethod(parameter);
            if (methodDescriptor == null)
            {
                if (this.TargetObject != null)
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.CurrentCulture,
                        "Cannot find method named {0} on object of type {1} that matches the expected signature.",
                        this.MethodName,
                        this.targetObjectType));
                }

                return false;
            }

            ParameterInfo[] parameters = methodDescriptor.Parameters;
            if (parameters.Length == 0)
            {
                methodDescriptor.MethodInfo.Invoke(target, parameters: null);
                return true;
            }
            else if (parameters.Length == 2)
            {
                methodDescriptor.MethodInfo.Invoke(target, new object[] { target, parameter });
                return true;
            }

            return false;
        }

        private MethodDescriptor FindBestMethod(object parameter)
        {
            TypeInfo parameterTypeInfo = parameter == null ? null : parameter.GetType().GetTypeInfo();

            if (parameterTypeInfo == null)
            {
                return this.cachedMethodDescriptor;
            }

            MethodDescriptor mostDerivedMethod = null;

            // Loop over the methods looking for the one whose type is closest to the type of the given parameter.
            foreach (MethodDescriptor currentMethod in this.methodDescriptors)
            {
                TypeInfo currentTypeInfo = currentMethod.SecondParameterTypeInfo;

                if (currentTypeInfo.IsAssignableFrom(parameterTypeInfo))
                {
                    if (mostDerivedMethod == null || !currentTypeInfo.IsAssignableFrom(mostDerivedMethod.SecondParameterTypeInfo))
                    {
                        mostDerivedMethod = currentMethod;
                    }
                }
            }

            return mostDerivedMethod ?? this.cachedMethodDescriptor;
        }

        private void UpdateTargetType(Type newTargetType)
        {
            if (newTargetType == this.targetObjectType)
            {
                return;
            }

            this.targetObjectType = newTargetType;

            this.UpdateMethodDescriptors();
        }

        private void UpdateMethodDescriptors()
        {
            this.methodDescriptors.Clear();
            this.cachedMethodDescriptor = null;

            if (string.IsNullOrEmpty(this.MethodName) || this.targetObjectType == null)
            {
                return;
            }

            // Find all public methods that match the given name  and have either no parameters,
            // or two parameters where the first is of type Object.
            foreach (MethodInfo method in this.targetObjectType.GetRuntimeMethods())
            {
                if (string.Equals(method.Name, this.MethodName, StringComparison.Ordinal)
                    && method.ReturnType == typeof(void)
                    && method.IsPublic)
                {
                    ParameterInfo[] parameters = method.GetParameters();
                    if (parameters.Length == 0)
                    {
                        // There can be only one parameterless method of the given name.
                        this.cachedMethodDescriptor = new MethodDescriptor(method, parameters);
                    }
                    else if (parameters.Length == 2 && parameters[0].ParameterType == typeof(object))
                    {
                        this.methodDescriptors.Add(new MethodDescriptor(method, parameters));
                    }
                }
            }

            // We didn't find a parameterless method, so we want to find a method that accepts null
            // as a second parameter, but if we have more than one of these it is ambiguous which
            // we should call, so we do nothing.
            if (this.cachedMethodDescriptor == null)
            {
                foreach (MethodDescriptor method in this.methodDescriptors)
                {
                    TypeInfo typeInfo = method.SecondParameterTypeInfo;
                    if (!typeInfo.IsValueType || (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>)))
                    {
                        if (this.cachedMethodDescriptor != null)
                        {
                            this.cachedMethodDescriptor = null;
                            return;
                        }
                        else
                        {
                            this.cachedMethodDescriptor = method;
                        }
                    }
                }
            }
        }

        [DebuggerDisplay("{MethodInfo}")]
        private class MethodDescriptor
        {
            public MethodDescriptor(MethodInfo methodInfo, ParameterInfo[] methodParameters)
            {
                this.MethodInfo = methodInfo;
                this.Parameters = methodParameters;
            }

            public MethodInfo MethodInfo { get; private set; }

            public ParameterInfo[] Parameters { get; private set; }

            public int ParameterCount
            {
                get { return this.Parameters.Length; }
            }

            public TypeInfo SecondParameterTypeInfo
            {
                get
                {
                    if (this.ParameterCount < 2)
                    {
                        return null;
                    }

                    return this.Parameters[1].ParameterType.GetTypeInfo();
                }
            }
        }
    }
}
