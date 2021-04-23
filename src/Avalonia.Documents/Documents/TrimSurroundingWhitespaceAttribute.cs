// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
//  Microsoft Windows Client Platform
//
//
//  Description: Specifies that the whitespace surrounding an element should be trimmed.
//
//  Created:     02/06/2006
//

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{

    /// <summary>
    /// An attribute that specifies that the whitespace surrounding an element should be trimmed
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple=false, Inherited=true)]
    public sealed class TrimSurroundingWhitespaceAttribute : Attribute
    {
        /// <summary>
        /// Creates a new trim surrounding whitespace attribute.
        /// </summary>
        public TrimSurroundingWhitespaceAttribute()
        {
        }
   }
}
