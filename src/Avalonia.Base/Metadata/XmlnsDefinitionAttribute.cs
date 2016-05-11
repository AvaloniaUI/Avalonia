// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Metadata
{
    /// <summary>
    /// Maps an XML namespace to a CLR namespace for use in XAML.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public class XmlnsDefinitionAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="XmlnsDefinitionAttribute"/> class.
        /// </summary>
        /// <param name="xmlNamespace">The URL of the XML namespace.</param>
        /// <param name="clrNamespace">The CLR namespace.</param>
        public XmlnsDefinitionAttribute(string xmlNamespace, string clrNamespace)
        {
            XmlNamespace = xmlNamespace;
            ClrNamespace = clrNamespace;
        }

        /// <summary>
        /// Gets or sets the URL of the XML namespace.
        /// </summary>
        public string XmlNamespace { get; }

        /// <summary>
        /// Gets or sets the CLR namespace.
        /// </summary>
        public string ClrNamespace { get; }
    }
}
