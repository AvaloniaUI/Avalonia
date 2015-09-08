





namespace Perspex.Markup.Xaml.DataBinding
{
    public class TargetBindingEndpoint
    {
        public PerspexObject Object { get; }

        public PerspexProperty Property { get; }

        public TargetBindingEndpoint(PerspexObject obj, PerspexProperty property)
        {
            this.Object = obj;
            this.Property = property;
        }
    }
}