// -----------------------------------------------------------------------
// <copyright file="XamlBindingDefinition.cs" company="Steven Kirk">
// Copyright 2015 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Markup.Xaml.DataBinding
{
    using ChangeTracking;
    using Controls;

    public class XamlBindingDefinition
    {
        private PropertyPath sourcePropertyPath;
        private BindingMode bindingMode;
        private readonly Control target;
        private readonly PerspexProperty targetProperty;

        public XamlBindingDefinition(Control target, PerspexProperty targetProperty, PropertyPath sourcePropertyPath, BindingMode bindingMode)
        {
            this.target = target;
            this.targetProperty = targetProperty;
            this.sourcePropertyPath = sourcePropertyPath;
            this.bindingMode = bindingMode;
        }

        public Control Target => this.target;

        public PerspexProperty TargetProperty => this.targetProperty;

        public PropertyPath SourcePropertyPath => this.sourcePropertyPath;

        public BindingMode BindingMode => this.bindingMode;
    }
}