// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Perspex.Xaml.Interactivity
{
    /// <summary>
    /// Associates the given editor type with the property to which the <see cref="CustomPropertyValueEditor"/> is applied.
    /// </summary>
    /// <remarks>Use this attribute to get improved design-time editing for properties that denote element (by name), storyboards, or states (by name).</remarks>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public sealed class CustomPropertyValueEditorAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomPropertyValueEditorAttribute"/> class.
        /// </summary>
        /// <param name="customPropertyValueEditor">The custom property value editor.</param>
        public CustomPropertyValueEditorAttribute(CustomPropertyValueEditor customPropertyValueEditor)
        {
            this.CustomPropertyValueEditor = customPropertyValueEditor;
        }

        /// <summary>
        /// Gets the custom property value editor.
        /// </summary>
        public CustomPropertyValueEditor CustomPropertyValueEditor { get; private set; }
    }
}
