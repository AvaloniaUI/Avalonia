// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Moq;
using Perspex.Markup.Xaml.Binding;
using OmniXaml.TypeConversion;
using Xunit;

namespace Perspex.Xaml.Base.UnitTest
{
    public class BinderTest
    {
        [Fact]
        public void NullTarget_Throws()
        {
            var typeConverter = new Mock<ITypeConverterProvider>();
            var perspexPropertyBinder = new PerspexPropertyBinder(typeConverter.Object);
            var bindingDefinitionBuilder = new BindingDefinitionBuilder();
            var binding = bindingDefinitionBuilder
                .WithNullTarget()
                .Build();

            var exception = Assert.Throws<InvalidOperationException>(() => perspexPropertyBinder.Create(binding));
        }
    }
}
