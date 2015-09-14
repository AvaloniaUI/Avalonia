// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Markup.Xaml.DataBinding;
using Perspex.Markup.Xaml.DataBinding.ChangeTracking;

namespace Perspex.Xaml.Base.UnitTest
{
    public class BindingDefinitionBuilder
    {
        private readonly BindingMode _bindingMode;
        private readonly PropertyPath _sourcePropertyPath;
        private Control _target;
        private PerspexProperty _targetProperty;

        public BindingDefinitionBuilder()
        {
            _bindingMode = BindingMode.Default;
            _sourcePropertyPath = new PropertyPath(string.Empty);
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
                targetProperty: _targetProperty);
        }
    }
}