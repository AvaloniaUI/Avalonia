// This source file is adapted from the Windows Presentation Foundation project. 
// (https://github.com/dotnet/wpf/) 
// 
// Licensed to The Avalonia Project under MIT License, courtesy of The .NET Foundation.

using System;

#nullable enable

namespace Avalonia.Controls.Metadata
{
    /// <summary>
    /// Defines a control template part referenced by name in code.
    /// Template part names should begin with the "PART_" prefix.
    /// </summary>
    /// <remarks>
    /// Style authors should be able to identify the part type used for styling the specific control.
    /// The part is usually required in the style and should have a specific predefined name.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class TemplatePartAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatePartAttribute"/> class.
        /// </summary>
        public TemplatePartAttribute()
        {
            Name = string.Empty;
            Type = typeof(object);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplatePartAttribute"/> class.
        /// </summary>
        /// <param name="name">The part name used by the class to identify a required element in the style.</param>
        /// <param name="type">The type of the element that should be used as a part with name.</param>
        public TemplatePartAttribute(string name, Type type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets or sets the part name used by the class to identify a required element in the style.
        /// Template part names should begin with the "PART_" prefix.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the type of the element that should be used as a part with name specified
        /// in <see cref="Name"/>.
        /// </summary>
        public Type Type { get; set; }
    }
}
