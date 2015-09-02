using System;

namespace Perspex.Xaml.Base.UnitTest
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
            set { this.SetValue(IntProperty, value); }
        }

        public string String
        {
            get { return GetValue(StringProperty); }
            set { this.SetValue(StringProperty, value); }
        }
    }
}