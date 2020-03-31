using System;
using Avalonia.Styling;

namespace Avalonia.Styling.UnitTests
{
    public static class TestSelectors
    {
        public static Selector SubscribeCheck(this Selector selector)
        {
            throw new NotImplementedException();
            //return new Selector(
            //    selector,
            //    control => new SelectorMatch(((TestControlBase)control).SubscribeCheckObservable),
            //    "");
        }
    }
}
