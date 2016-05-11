// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Avalonia.Markup.Xaml.UnitTests
{
    internal class SampleAvaloniaObject : AvaloniaObject
    {
        public static readonly StyledProperty<string> StringProperty =
            AvaloniaProperty.Register<AvaloniaObject, string>("StrProp", string.Empty);

        public static readonly StyledProperty<int> IntProperty =
            AvaloniaProperty.Register<AvaloniaObject, int>("IntProp");

        public int Int
        {
            get { return GetValue(IntProperty); }
            set { SetValue(IntProperty, value); }
        }

        public string String
        {
            get { return GetValue(StringProperty); }
            set { SetValue(StringProperty, value); }
        }
    }
}