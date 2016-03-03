// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using Perspex.Controls;

namespace Perspex.Markup.Xaml.UnitTests.Xaml
{
    public class NonControl : PerspexObject
    {
        public static readonly StyledProperty<Control> ControlProperty =
            PerspexProperty.Register<NonControl, Control>("Control");

        public static readonly StyledProperty<string> StringProperty =
            PerspexProperty.Register<NonControl, string>("String");

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
