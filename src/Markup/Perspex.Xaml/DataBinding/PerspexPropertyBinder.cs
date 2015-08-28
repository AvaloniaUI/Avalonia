namespace Perspex.Xaml.DataBinding
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using OmniXaml.TypeConversion;

    public class PerspexPropertyBinder : IPerspexPropertyBinder
    {
        private readonly ITypeConverterProvider typeConverterProvider;

        private readonly HashSet<XamlBinding> bindings;

        public PerspexPropertyBinder(ITypeConverterProvider typeConverterProvider)
        {
            this.typeConverterProvider = typeConverterProvider;
            bindings = new HashSet<XamlBinding>();
        }

        public XamlBinding GetBinding(PerspexObject po, PerspexProperty pp)
        {
            return bindings.First(xamlBinding => xamlBinding.Target == po && xamlBinding.TargetProperty == pp);
        }

        public IEnumerable<XamlBinding> GetBindings(PerspexObject source)
        {
            return from binding in bindings
                where binding.Target == source
                select binding;
        }

        public XamlBinding Create(XamlBindingDefinition xamlBinding)
        {
            if (xamlBinding.Target == null)
            {
                throw new InvalidOperationException();
            }

            if (xamlBinding.TargetProperty == null)
            {
                throw new InvalidOperationException();
            }

            var binding = new XamlBinding(typeConverterProvider)
            {
                BindingMode = xamlBinding.BindingMode,
                SourcePropertyPath = xamlBinding.SourcePropertyPath,
                Target = xamlBinding.Target,
                TargetProperty = xamlBinding.TargetProperty
            };

            bindings.Add(binding);
            return binding;
        }
    }
}