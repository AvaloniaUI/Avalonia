namespace Perspex.Markup.Xaml.Context
{
    using System;
    using DataBinding;
    using OmniXaml;
    using OmniXaml.Typing;

    public class PerspexXamlType : XamlType
    {
        private readonly IPerspexPropertyBinder propertyBinder;

        public PerspexXamlType(Type type,
            IXamlTypeRepository typeRepository,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider,
            IPerspexPropertyBinder propertyBinder) : base(type, typeRepository, typeFactory, featureProvider)
        {
            this.propertyBinder = propertyBinder;
        }

        protected IPerspexPropertyBinder PropertyBinder => propertyBinder;

        protected override XamlMember LookupMember(string name)
        {
            return new PerspexXamlMember(name, this, TypeRepository, FeatureProvider, propertyBinder);
        }

        public override string ToString()
        {
            return "Perspex XAML Type " + base.ToString();
        }
    }
}