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

        public Control Target => target;

        public PerspexProperty TargetProperty => targetProperty;

        public PropertyPath SourcePropertyPath => sourcePropertyPath;

        public BindingMode BindingMode => bindingMode;
    }
}