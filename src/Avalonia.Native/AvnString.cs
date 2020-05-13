using System.Runtime.InteropServices;

namespace Avalonia.Native.Interop
{
    unsafe partial class IAvnString
    {
        private string _managed;

        public string String
        {
            get
            {
                if (_managed == null)
                {
                    var ptr = Pointer();
                    if (ptr == null)
                        return null;
                    _managed = System.Text.Encoding.UTF8.GetString((byte*)ptr.ToPointer(), Length());
                }

                return _managed;
            }
        }

        public override string ToString() => String;
    }
    
    partial class IAvnStringArray
    {
        public string[] ToStringArray()
        {
            var arr = new string[Count];
            for(uint c = 0; c<arr.Length;c++)
                using (var s = Get(c))
                    arr[c] = s.String;
            return arr;
        }
    }
}
