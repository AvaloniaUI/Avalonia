// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Linq;
using Moq;
using Perspex.Markup.Xaml.Data;
using Perspex.Platform;
using Perspex.Styling;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests
{
    public class StyleTests
    {
        [Fact]
        public void Binding_Should_Be_Assigned_To_Setter_Value_Instead_Of_Bound()
        {
            using (PerspexLocator.EnterScope())
            {
                PerspexLocator.CurrentMutable
                    .Bind<IPclPlatformWrapper>()
                    .ToConstant(Mock.Of<IPclPlatformWrapper>());

                var xaml = "<Style xmlns='https://github.com/perspex'><Setter Value='{Binding}'/></Style>";
                var loader = new PerspexXamlLoader();
                var style = (Style)loader.Load(xaml);
                var setter = (Setter)(style.Setters.First());

                Assert.IsType<Binding>(setter.Value);
            }                
        }
    }
}
