// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/***************************************************************************\
*
*
* Description:
*
*
\***************************************************************************/

using System.ComponentModel;
using System.Windows.Markup;
using System;
using MS.Internal;

namespace System.Windows.Markup 
{
    ///<summary>
    /// The IAddChild interface is used for parsing objects that
    /// allow objects or text underneath their tags in markup that
    /// do not map directly to a property. 
    ///</summary>
    //[Obsolete("IAddChild is obsolete, use the [ContentProperty] attribute instead")]
    public interface IAddChild
    {
        ///<summary>
        /// Called to Add the object as a Child.
        ///</summary>
        ///<param name="value">
        /// Object to add as a child
        ///</param>
        void AddChild(Object value);

        ///<summary>
        /// Called when text appears under the tag in markup
        ///</summary>
        ///<param name="text">
        /// Text to Add to the Object
        ///</param> 
        void AddText(string text);
    }

    internal interface IAddChildInternal : IAddChild
    {
    }
}
