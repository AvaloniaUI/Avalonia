// -----------------------------------------------------------------------
// <copyright file="PerspexPropertyBinder.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.DataBinding
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
            this.bindings = new HashSet<XamlBinding>();
        }

        public XamlBinding GetBinding(PerspexObject po, PerspexProperty pp)
        {
            return this.bindings.First(xamlBinding => xamlBinding.Target == po && xamlBinding.TargetProperty == pp);
        }

        public IEnumerable<XamlBinding> GetBindings(PerspexObject source)
        {
            return from binding in this.bindings
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

            var binding = new XamlBinding(this.typeConverterProvider)
            {
                BindingMode = xamlBinding.BindingMode,
                SourcePropertyPath = xamlBinding.SourcePropertyPath,
                Target = xamlBinding.Target,
                TargetProperty = xamlBinding.TargetProperty
            };

            this.bindings.Add(binding);
            return binding;
        }
    }
}