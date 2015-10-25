// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Moq;
using Perspex.Controls;
using Perspex.Markup.Xaml.Data;
using Perspex.Styling;
using Xunit;

namespace Perspex.Markup.Xaml.UnitTests.Data
{
    public class BindingTests_TemplatedParent
    {
        [Fact]
        public void OneWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Mode = BindingMode.OneWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Priority = BindingPriority.TemplatedParent,
                SourcePropertyPath = "Foo",
            };

            binding.Bind(target.Object, TextBox.TextProperty);

            target.Verify(x => x.Bind(
                TextBox.TextProperty, 
                It.IsAny<IObservable<object>>(), 
                BindingPriority.TemplatedParent));
        }

        [Fact]
        public void TwoWay_Binding_Should_Be_Set_Up()
        {
            var target = CreateTarget();
            var binding = new Binding
            {
                Mode = BindingMode.TwoWay,
                RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                Priority = BindingPriority.TemplatedParent,
                SourcePropertyPath = "Foo",
            };

            binding.Bind(target.Object, TextBox.TextProperty);

            target.Verify(x => x.BindTwoWay(
                TextBox.TextProperty,
                It.IsAny<ISubject<object>>(),
                BindingPriority.TemplatedParent));
        }

        [Fact]
        public void OneWayToSource_Binding_Should_Be_Set_Up()
        {
            var textObservable = new Mock<IObservable<string>>();
            var expression = new Mock<ISubject<object>>();
            var target = CreateTarget(text: textObservable.Object);
            var binding = new Binding
            {
                SourcePropertyPath = "Foo",
                Mode = BindingMode.OneWayToSource,
            };

            binding.Bind(target.Object, TextBox.TextProperty, expression.Object);

            textObservable.Verify(x => x.Subscribe(expression.Object));
        }

        private Mock<IObservablePropertyBag> CreateTarget(ITemplatedControl templatedParent)
        {
            return CreateTarget(templatedParent: Observable.Never<ITemplatedControl>().StartWith(templatedParent));
        }

        private Mock<IObservablePropertyBag> CreateTarget(
            IObservable<ITemplatedControl> templatedParent = null,
            IObservable<string> text = null)
        {
            var result = new Mock<IObservablePropertyBag>();

            templatedParent = templatedParent ?? Observable.Never<ITemplatedControl>().StartWith((ITemplatedControl)null);
            text = text ?? Observable.Never<string>().StartWith((string)null);

            result.Setup(x => x.GetObservable(Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetObservable((PerspexProperty)Control.TemplatedParentProperty)).Returns(templatedParent);
            result.Setup(x => x.GetObservable((PerspexProperty)TextBox.TextProperty)).Returns(text);
            return result;
        }
    }
}
