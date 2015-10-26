namespace Perspex.Markup.Xaml.Data
{
    public interface IBinding
    {
        void Bind(IObservablePropertyBag instance, PerspexProperty property);
    }
}