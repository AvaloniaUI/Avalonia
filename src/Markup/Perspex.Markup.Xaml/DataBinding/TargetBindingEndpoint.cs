// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Xaml.DataBinding
{
    public class TargetBindingEndpoint
    {
        public PerspexObject Object { get; }

        public PerspexProperty Property { get; }

        public TargetBindingEndpoint(PerspexObject obj, PerspexProperty property)
        {
            Object = obj;
            Property = property;
        }
    }
}