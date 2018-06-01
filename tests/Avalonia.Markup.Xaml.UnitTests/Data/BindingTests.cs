// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Markup.Data;
using Moq;
using Xunit;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.UnitTests;
using Avalonia.Data.Converters;
using Avalonia.Data.Core;

namespace Avalonia.Markup.Xaml.UnitTests.Data
{
    public class BindingTests
    {
        [Fact]
        public void Binding_With_Null_Path_Works()
        {
            using (UnitTestApplication.Start(TestServices.StyledWindow))
            {
                var xaml = @"
<Window xmlns='https://github.com/avaloniaui'
        xmlns:x='http://schemas.microsoft.com/winfx/2006/xaml'>
    <TextBlock Name='textBlock' Text='{Binding}'/>
</Window>";
                var loader = new AvaloniaXamlLoader();
                var window = (Window)loader.Load(xaml);
                var textBlock = window.FindControl<TextBlock>("textBlock");

                window.DataContext = "foo";
                window.ApplyTemplate();

                Assert.Equal("foo", textBlock.Text);
            }
        }
    }
}
