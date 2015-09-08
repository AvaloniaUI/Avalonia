





namespace Perspex.Markup.Xaml.Context
{
    using System;
    using DataBinding;
    using Glass;
    using OmniXaml;
    using OmniXaml.Typing;

    public class PerspexTypeRepository : XamlTypeRepository
    {
        private readonly ITypeFactory typeFactory;
        private readonly IPerspexPropertyBinder propertyBinder;

        public PerspexTypeRepository(IXamlNamespaceRegistry xamlNamespaceRegistry,
            ITypeFactory typeFactory,
            ITypeFeatureProvider featureProvider,
            IPerspexPropertyBinder propertyBinder) : base(xamlNamespaceRegistry, typeFactory, featureProvider)
        {
            this.typeFactory = typeFactory;
            this.propertyBinder = propertyBinder;
        }

        public override XamlType GetXamlType(Type type)
        {
            Guard.ThrowIfNull(type, nameof(type));
            return new PerspexXamlType(type, this, this.typeFactory, this.FeatureProvider, this.propertyBinder);
        }
    }
}