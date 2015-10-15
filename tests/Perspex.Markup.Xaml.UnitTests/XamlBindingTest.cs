// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Perspex.Controls;
using Perspex.Markup.Xaml.Binding;
using Xunit;

namespace Perspex.Xaml.Base.UnitTest
{
    public class XamlBindingTest
    {
        /// <summary>
        /// Tests a problem discovered with ListBox with selection.
        /// </summary>
        /// <remarks>
        /// - Items is bound to DataContext first, followed by say SelectedIndex
        /// - When the ListBox is removed from the visual tree, DataContext becomes null (as it's
        ///   inherited)
        /// - This changes Items to null, which changes SelectedIndex to null as there are no
        ///   longer any items
        /// - However, the news that DataContext is now null hasn't yet reached the SelectedIndex
        ///   binding and so the unselection is sent back to the ViewModel
        /// </remarks>
        [Fact]
        public void Should_Not_Write_To_Old_DataContext()
        {
            var vm = new OldDataContextViewModel();
            var target = new OldDataContextTest();

            var fooBinding = new XamlBinding
            {
                SourcePropertyPath = "Foo",
                BindingMode = BindingMode.TwoWay,
            };

            var barBinding = new XamlBinding
            {
                SourcePropertyPath = "Bar",
                BindingMode = BindingMode.TwoWay,
            };

            // Bind Foo and Bar to the VM.
            fooBinding.Bind(target, OldDataContextTest.FooProperty);
            barBinding.Bind(target, OldDataContextTest.BarProperty);
            target.DataContext = vm;

            // Make sure the control's Foo and Bar properties are read from the VM
            Assert.Equal(1, target.GetValue(OldDataContextTest.FooProperty));
            Assert.Equal(2, target.GetValue(OldDataContextTest.BarProperty));

            // Set DataContext to null.
            target.DataContext = null;

            // Foo and Bar are no longer bound so they return 0, their default value.
            Assert.Equal(0, target.GetValue(OldDataContextTest.FooProperty));
            Assert.Equal(0, target.GetValue(OldDataContextTest.BarProperty));

            // The problem was here - DataContext is now null, setting Foo to 0. Bar is bound to 
            // Foo so Bar also gets set to 0. However the Bar binding still had a reference to
            // the VM and so vm.Bar was set to 0 erroneously.
            Assert.Equal(1, vm.Foo);
            Assert.Equal(2, vm.Bar);
        }

        private class OldDataContextViewModel
        {
            public int Foo { get; set; } = 1;
            public int Bar { get; set; } = 2;
        }

        private class OldDataContextTest : Control
        {
            public static readonly PerspexProperty<int> FooProperty =
                PerspexProperty.Register<OldDataContextTest, int>("Foo");

            public static readonly PerspexProperty<int> BarProperty =
              PerspexProperty.Register<OldDataContextTest, int>("Bar");

            public OldDataContextTest()
            {
                Bind(BarProperty, GetObservable(FooProperty));
            }
        }
    }
}
