// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Perspex.Xaml.Interactivity
{
    /// <summary>
    /// Specifies type constraints on the AssociatedObject of  <see cref="IBehavior"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class TypeConstraintAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TypeConstraintAttribute"/> class.
        /// </summary>
        /// <param name="constraint">The constraint type.</param>
        public TypeConstraintAttribute(Type constraint)
        {
            this.Constraint = constraint;
        }

        /// <summary>
        /// Gets the constraint type.
        /// </summary>
        public Type Constraint { get; private set; }
    }
}
