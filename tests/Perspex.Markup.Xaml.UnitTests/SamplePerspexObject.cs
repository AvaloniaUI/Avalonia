// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;

namespace Perspex.Markup.Xaml.UnitTests
{
    internal class SamplePerspexObject : PerspexObject
    {
        public static readonly PerspexProperty<string> StringProperty =
            PerspexProperty.Register<PerspexObject, string>("StrProp", string.Empty);

        public static readonly PerspexProperty<int> IntProperty =
            PerspexProperty.Register<PerspexObject, int>("IntProp");

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