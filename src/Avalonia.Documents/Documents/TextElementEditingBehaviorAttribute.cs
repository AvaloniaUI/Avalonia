// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace System.Windows.Documents
{
    /// <summary>
    /// An attribute that controls editing behavior of elements.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class TextElementEditingBehaviorAttribute : System.Attribute
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public TextElementEditingBehaviorAttribute()
        {
        }

        /// <summary>
        /// If true, the element can be merged with other elements of the same type when
        /// properties are the same.  This also affects other aspects of editing around the
        /// element.  If true and the element is at the end of the document, there is no
        /// insertion position outside the element; if false under these conditions, there is
        /// no insertion position inside instead.  An empty mergeable element at the start of
        /// the document will be preserved; an empty non-mergeable element will be discarded.
        /// A mergeable element can be split by inserting a paragraph break inside; a
        /// non-mergeable cannot, and the editor will not allow a break to be inserted.
        /// </summary>
        public bool IsMergeable
        {
            get { return _isMergeable; }
            set { _isMergeable = value; }
        }

        /// <summary>
        /// If true, the element has only typographic meaning-- it exists solely to format
        /// content.  If false, the element has contextual meaning or UI behavior that would
        /// make no sense to carry over into a new context that doesn't know how to handle
        /// that behavior.
        /// 
        /// When an element is partially selected and copied, formatting will be lost on the
        /// new copy if IsTypographicOnly is false (e.g. Hyperlink).  If true, formatting will
        /// persist.
        /// </summary>
        public bool IsTypographicOnly
        {
            get { return _isTypographicOnly; }
            set { _isTypographicOnly = value; }
        }

        private bool _isMergeable;
        private bool _isTypographicOnly;
    }
}