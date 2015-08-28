// -----------------------------------------------------------------------
// <copyright file="IPerspexPropertyBinder.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.DataBinding
{
    using System.Collections.Generic;

    public interface IPerspexPropertyBinder
    {
        XamlBinding GetBinding(PerspexObject po, PerspexProperty pp);

        IEnumerable<XamlBinding> GetBindings(PerspexObject source);

        XamlBinding Create(XamlBindingDefinition xamlBinding);
    }
}