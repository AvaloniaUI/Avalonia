// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reflection;
using Avalonia.Controls.Templates;

namespace Avalonia.Markup.Xaml.Templates
{
    public class MemberSelector : IMemberSelector
    {
        public string MemberName { get; set; }

        public object Select(object o)
        {
            // TODO: Handle nested property paths, changing values etc.
            var property = o.GetType().GetRuntimeProperty(MemberName);
            return property?.GetValue(o);
        }
    }
}
