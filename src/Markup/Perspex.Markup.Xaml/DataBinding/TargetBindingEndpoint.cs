// -----------------------------------------------------------------------
// <copyright file="TargetBindingEndpoint.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.DataBinding
{
    public class TargetBindingEndpoint
    {
        public PerspexObject Object { get; }

        public PerspexProperty Property { get; }

        public TargetBindingEndpoint(PerspexObject obj, PerspexProperty property)
        {
            this.Object = obj;
            this.Property = property;
        }
    }
}