namespace Perspex.Xaml.DataBinding
{
    using System.Collections.Generic;

    public interface IPerspexPropertyBinder
    {
        XamlBinding GetBinding(PerspexObject po, PerspexProperty pp);
        IEnumerable<XamlBinding> GetBindings(PerspexObject source);
        XamlBinding Create(XamlBindingDefinition xamlBinding);
    }
}