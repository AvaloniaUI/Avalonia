namespace Perspex.Xaml.Context
{
    using DataBinding;
    using OmniXaml;
    using OmniXaml.Typing;

    public class PerspexXamlMember : XamlMember
    {
        private readonly IPerspexPropertyBinder propertyBinder;

        public PerspexXamlMember(string name,
            XamlType owner,
            IXamlTypeRepository xamlTypeRepository,
            ITypeFeatureProvider featureProvider,
            IPerspexPropertyBinder propertyBinder)
            : base(name, owner, xamlTypeRepository, featureProvider)
        {
            this.propertyBinder = propertyBinder;
        }

        protected override IXamlMemberValuePlugin LookupXamlMemberValueConnector()
        {
            return new PerspexXamlMemberValuePlugin(this, propertyBinder);
        }

        public override string ToString()
        {
            return "Perspex XAML Member " + base.ToString();
        }
    }
}