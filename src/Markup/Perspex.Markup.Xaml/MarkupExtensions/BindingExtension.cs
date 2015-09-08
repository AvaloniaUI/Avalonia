





namespace Perspex.Markup.Xaml.MarkupExtensions
{
    using System.Linq;
    using Controls;
    using DataBinding;
    using DataBinding.ChangeTracking;
    using OmniXaml;

    public class BindingExtension : MarkupExtension
    {
        public BindingExtension()
        {
        }

        public BindingExtension(string path)
        {
            this.Path = path;
        }

        public override object ProvideValue(MarkupExtensionContext extensionContext)
        {
            var target = extensionContext.TargetObject as Control;
            var targetProperty = extensionContext.TargetProperty;
            var targetPropertyName = targetProperty.Name;
            var perspexProperty = target.GetRegisteredProperties().First(property => property.Name == targetPropertyName);

            return new XamlBindingDefinition
                (
                target,
                perspexProperty,
                new PropertyPath(this.Path),
                this.Mode == BindingMode.Default ? BindingMode.OneWay : this.Mode
                );
        }

        /// <summary> The source path (for CLR bindings).</summary>
        public string Path { get; set; }

        public BindingMode Mode { get; set; }
    }
}