// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Avalonia.Controls;

namespace Avalonia.Markup.Xaml.UnitTests.Xaml
{
    public class NonControl : AvaloniaObject
    {
        public static readonly StyledProperty<Control> ControlProperty =
            AvaloniaProperty.Register<NonControl, Control>("Control");

        public static readonly StyledProperty<string> StringProperty =
            AvaloniaProperty.Register<NonControl, string>("String");

        public Control Control
        {
            get { return GetValue(ControlProperty); }
            set { SetValue(ControlProperty, value); }
        }

        public string String
        {
            get { return GetValue(StringProperty); }
            set { SetValue(StringProperty, value); }
        }
    }
}
