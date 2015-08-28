namespace Perspex.Xaml.HighLevel
{
    using OmniXaml;
    using Context;
    using OmniXaml.ObjectAssembler;

    public class PerspexObjectAssemblerFactory : IObjectAssemblerFactory
    {
        private readonly WiringContext context;

        public PerspexObjectAssemblerFactory(WiringContext context)
        {
            this.context = context;
        }

        public IObjectAssembler CreateAssembler(ObjectAssemblerSettings settings)
        {
            return new PerspexObjectAssembler(context, settings);
        }
    }
}