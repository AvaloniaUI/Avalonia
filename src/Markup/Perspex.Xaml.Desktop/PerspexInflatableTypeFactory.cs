namespace Perspex.Xaml.Desktop
{
    using System;
    using System.Collections.ObjectModel;
    using Controls;
    using OmniXaml;
    using OmniXaml.AppServices;
    using OmniXaml.AppServices.NetCore;

    public class PerspexInflatableTypeFactory : InflatableTypeFactory
    {
        public PerspexInflatableTypeFactory() : base(new TypeFactory(), new InflatableResourceTranslator(), typeFactory => new PerspexXamlLoader(typeFactory))
        {
            Inflatables = new Collection<Type> { typeof(Window), typeof(UserControl) };
        }
    }
}