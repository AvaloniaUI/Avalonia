using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    public abstract class ElementFactory : IElementFactory
    {
        public IControl Build(object? data)
        {
            return GetElementCore(new ElementFactoryGetArgs { Data = data });
        }

        public IControl GetElement(ElementFactoryGetArgs args)
        {
            return GetElementCore(args);
        }

        public bool Match(object? data) => true;

        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            RecycleElementCore(args);
        }

        protected abstract IControl GetElementCore(ElementFactoryGetArgs args);
        protected abstract void RecycleElementCore(ElementFactoryRecycleArgs args);
    }
}
