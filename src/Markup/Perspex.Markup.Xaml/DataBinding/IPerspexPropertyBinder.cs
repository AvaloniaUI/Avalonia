// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Collections.Generic;

namespace Perspex.Markup.Xaml.DataBinding
{
    public interface IPerspexPropertyBinder
    {
        XamlBinding GetBinding(PerspexObject po, PerspexProperty pp);

        IEnumerable<XamlBinding> GetBindings(PerspexObject source);

        XamlBinding Create(XamlBindingDefinition xamlBinding);
    }
}