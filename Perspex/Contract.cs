using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Perspex
{
    internal static class Contract
    {
        public static void Requires<TException>(bool condition) where TException : Exception, new()
        {
#if DEBUG
            if (!condition)
            {
                throw new TException();
            }
#endif
        }
    }
}
