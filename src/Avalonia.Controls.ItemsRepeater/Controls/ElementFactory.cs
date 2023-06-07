using Avalonia.Controls.Templates;

namespace Avalonia.Controls
{
    public abstract class ElementFactory : IElementFactory
    {
        public Control Build(object? data)
        {
            return GetElementCore(new ElementFactoryGetArgs { Data = data });
        }

        public Control GetElement(ElementFactoryGetArgs args)
        {
            return GetElementCore(args);
        }

        public bool Match(object? data) => true;

        public void RecycleElement(ElementFactoryRecycleArgs args)
        {
            RecycleElementCore(args);
        }

        protected abstract Control GetElementCore(ElementFactoryGetArgs args);
        protected abstract void RecycleElementCore(ElementFactoryRecycleArgs args);
    }
}
