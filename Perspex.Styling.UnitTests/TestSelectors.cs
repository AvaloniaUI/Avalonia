// -----------------------------------------------------------------------
// <copyright file="SubscribeCheck.cs" company="Steven Kirk">
// Copyright 2014 MIT Licence. See licence.md for more information.
// </copyright>
// -----------------------------------------------------------------------

namespace Perspex.Styling.UnitTests
{
    using Perspex.Styling;

    public static class TestSelectors
    {
        public static Selector SubscribeCheck(this Selector selector)
        {
            return new Selector(selector)
            {
                GetObservable = control => ((TestControlBase)control).SubscribeCheckObservable,
            };
        }
    }
}
