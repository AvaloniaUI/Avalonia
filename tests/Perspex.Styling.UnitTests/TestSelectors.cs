





namespace Perspex.Styling.UnitTests
{
    using Perspex.Styling;

    public static class TestSelectors
    {
        public static Selector SubscribeCheck(this Selector selector)
        {
            return new Selector(
                selector,
                control => new SelectorMatch(((TestControlBase)control).SubscribeCheckObservable),
                "");
        }
    }
}
