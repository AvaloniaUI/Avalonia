// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Moq;
using Perspex.Markup.Xaml.DataBinding;
using OmniXaml.TypeConversion;
using Xunit;

namespace Perspex.Xaml.Base.UnitTest
{
    public class XamlBindingTest
    {
        [Fact]
        public void TestNullDataContext()
        {
            var t = new Mock<ITypeConverterProvider>();
            var sut = new XamlBinding(t.Object);
            sut.BindToDataContext(null);
        }
    }
}
