// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System.Reactive.Subjects;
using Moq;
using Perspex.Controls;
using Perspex.Data;
using Xunit;

namespace Perspex.Styling.UnitTests
{
    public class SetterTests
    {
        [Fact]
        public void Setter_Should_Apply_Binding_To_Property()
        {
            var control = new TextBlock();
            var subject = new BehaviorSubject<object>("foo");
            var binding = Mock.Of<IBinding>(x => x.CreateSubject(control, TextBlock.TextProperty) == subject);
            var style = Mock.Of<IStyle>();
            var setter = new Setter(TextBlock.TextProperty, binding);

            setter.Apply(style, control, null);

            Assert.Equal("foo", control.Text);
        }
    }
}
