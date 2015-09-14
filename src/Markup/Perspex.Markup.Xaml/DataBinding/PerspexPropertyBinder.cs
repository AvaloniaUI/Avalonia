// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using OmniXaml.TypeConversion;

namespace Perspex.Markup.Xaml.DataBinding
{
    public class PerspexPropertyBinder : IPerspexPropertyBinder
    {
        private readonly ITypeConverterProvider _typeConverterProvider;

        private readonly HashSet<XamlBinding> _bindings;

        public PerspexPropertyBinder(ITypeConverterProvider typeConverterProvider)
        {
            _typeConverterProvider = typeConverterProvider;
            _bindings = new HashSet<XamlBinding>();
        }

        public XamlBinding GetBinding(PerspexObject po, PerspexProperty pp)
        {
            return _bindings.First(xamlBinding => xamlBinding.Target == po && xamlBinding.TargetProperty == pp);
        }

        public IEnumerable<XamlBinding> GetBindings(PerspexObject source)
        {
            return from binding in _bindings
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

            var binding = new XamlBinding(_typeConverterProvider)
            {
                BindingMode = xamlBinding.BindingMode,
                SourcePropertyPath = xamlBinding.SourcePropertyPath,
                Target = xamlBinding.Target,
                TargetProperty = xamlBinding.TargetProperty
            };

            _bindings.Add(binding);
            return binding;
        }
    }
}