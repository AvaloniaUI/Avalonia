using System;

namespace Perspex.Xaml.Base.UnitTest
{
    using Controls;
    using Markup.Xaml.DataBinding;
    using Markup.Xaml.DataBinding.ChangeTracking;

    public class BindingDefinitionBuilder
    {
        private readonly BindingMode bindingMode;
        private readonly PropertyPath sourcePropertyPath;
        private Control target;
        private PerspexProperty targetProperty;

        public BindingDefinitionBuilder()
        {
            bindingMode = BindingMode.Default;
            sourcePropertyPath = new PropertyPath(string.Empty);
        }

        public BindingDefinitionBuilder WithNullTarget()
        {
            target = null;
            return this;
        }

        public XamlBindingDefinition Build()
        {
            return new XamlBindingDefinition(
                bindingMode: bindingMode,
                sourcePropertyPath: sourcePropertyPath,
                target: target,
                targetProperty: targetProperty);
        }
    }
}