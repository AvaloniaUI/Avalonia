using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Avalonia.OpenGL
{

    public unsafe partial class GlInterface
    {

        public int GetIntegerv_one(GetPName pname)
        {
            var oneArr = new int[1];
            GetIntegerv(pname, oneArr);
            return oneArr[0];
        }

        public uint GenRenderbuffers_one(int n)
        {
            var oneArr = new uint[1];
            GenRenderbuffers(n, oneArr);
            return oneArr[0];
        }

    }

}