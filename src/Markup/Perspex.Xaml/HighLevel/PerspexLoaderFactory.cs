namespace Perspex.Xaml.HighLevel
{
    using Context;
    using OmniXaml;

    public static class PerspexWiringContextFactory
    {
        private static IWiringContext context;

        public static IWiringContext GetContext(ITypeFactory factory)
        {
            if (context == null)
            {
                context = new PerspexWiringContext(factory);
            }

            return context;
        }
    }
}