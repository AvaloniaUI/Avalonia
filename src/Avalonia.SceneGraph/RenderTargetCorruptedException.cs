using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia
{
    public class RenderTargetCorruptedException : Exception
    {
        public RenderTargetCorruptedException()
        {
        }

        public RenderTargetCorruptedException(string message)
            : base(message)
        {
        }

        public RenderTargetCorruptedException(Exception innerException)
            : base(null, innerException)
        {
        }

        public RenderTargetCorruptedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
