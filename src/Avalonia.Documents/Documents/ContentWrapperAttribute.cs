// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Microsoft Windows Client Platform
//
//
//  Description: Can be specified on a collection type to indicate which 
//               types are used to wrap content foreign content such as 
//               strings in a strongly type Collection<T>. 
//
//  Created:     06/07/2005 Microsoft
//

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// Can be specified on a collection type to indicate which 
    /// types are used to wrap content foreign content such as 
    /// strings in a strongly type Collection. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    sealed public class ContentWrapperAttribute : Attribute
    {
        /// <summary>
        /// Declares the given type as being a content wrapper for the collection 
        /// type this attribute is declared on.
        /// </summary>
        /// <param name="contentWrapper"></param>
        public ContentWrapperAttribute(Type contentWrapper)
        {
            ContentWrapper = contentWrapper;
        }

        /// <summary>
        /// The type that is declared as a content wrapper for the collection type
        /// this attribute is declared on.
        /// </summary>
        public Type ContentWrapper { get; }

        /// <summary>
        /// Override to ensure AttributeCollection perserves all instances
        /// </summary>
        public override object TypeId => this;

        /// <summary>
        ///     Overrides Object.Equals to implement correct equality semantics for this
        ///     attribute.
        /// </summary>
        public override bool Equals(object obj)
        {
            var other = obj as ContentWrapperAttribute;
            if (other == null) return false;
            return ContentWrapper == other.ContentWrapper;
        }

        /// <summary>
        ///     Overrides Object.GetHashCode to implement correct hashing semantics.
        /// </summary>
        public override int GetHashCode()
        {
            return ContentWrapper.GetHashCode();
        }
    }
}
