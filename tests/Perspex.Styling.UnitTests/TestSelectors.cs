// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Styling;

namespace Perspex.Styling.UnitTests
{
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
