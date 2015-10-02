// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Markup.Xaml.Binding;

namespace Perspex.Xaml.Base.UnitTest
{
    public class BindingDefinitionBuilder
    {
        private readonly BindingMode _bindingMode;
        private readonly string _sourcePropertyPath;
        private Control _target;

        public BindingDefinitionBuilder()
        {
            _bindingMode = BindingMode.Default;
            _sourcePropertyPath = string.Empty;
        }

        public BindingDefinitionBuilder WithNullTarget()
        {
            _target = null;
            return this;
        }

        public XamlBindingDefinition Build()
        {
            return new XamlBindingDefinition(
                bindingMode: _bindingMode,
                sourcePropertyPath: _sourcePropertyPath,
                target: _target,
                targetProperty: null);
        }
    }
}