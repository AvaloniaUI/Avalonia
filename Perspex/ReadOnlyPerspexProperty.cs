namespace Perspex
{
    public class ReadOnlyPerspexProperty<T>
    {
        public ReadOnlyPerspexProperty(PerspexProperty property)
        {
            this.Property = property;
        }

        internal PerspexProperty Property { get; private set; }
    }
}
