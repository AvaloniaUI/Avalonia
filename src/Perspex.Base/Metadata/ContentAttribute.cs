// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Metadata
{
    /// <summary>
    /// Defines the property that contains the object's content in markup.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class ContentAttribute : Attribute
    {
    }
}
