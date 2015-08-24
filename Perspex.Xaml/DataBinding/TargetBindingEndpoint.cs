namespace Perspex.Xaml.DataBinding
{
    public class TargetBindingEndpoint
    {
        public PerspexObject Object { get; }
        public PerspexProperty Property { get; }

        public TargetBindingEndpoint(PerspexObject obj, PerspexProperty property)
        {
            Object = obj;
            Property = property;
        }
    }
}