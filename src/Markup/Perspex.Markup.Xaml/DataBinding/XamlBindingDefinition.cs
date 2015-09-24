// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;
using Perspex.Markup.Xaml.DataBinding.ChangeTracking;

namespace Perspex.Markup.Xaml.DataBinding
{
    public class XamlBindingDefinition
    {
        private readonly PropertyPath _sourcePropertyPath;
        private readonly BindingMode _bindingMode;
        private readonly Control _target;
        private readonly PerspexProperty _targetProperty;

        public XamlBindingDefinition(Control target, PerspexProperty targetProperty, PropertyPath sourcePropertyPath, BindingMode bindingMode)
        {
            _target = target;
            _targetProperty = targetProperty;
            _sourcePropertyPath = sourcePropertyPath;
            _bindingMode = bindingMode;
        }

        public Control Target => _target;

        public PerspexProperty TargetProperty => _targetProperty;

        public PropertyPath SourcePropertyPath => _sourcePropertyPath;

        public BindingMode BindingMode => _bindingMode;
    }
}