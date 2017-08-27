// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace System.Windows.Markup
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class ContentPropertyAttribute : Attribute
    {

        public string Name { get; }

        public ContentPropertyAttribute()
        {
        }

        public ContentPropertyAttribute(string name)
        {
            Name = name;
        }
    }
}
