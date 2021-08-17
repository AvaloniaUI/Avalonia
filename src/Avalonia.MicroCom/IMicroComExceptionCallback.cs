using System;

namespace Avalonia.MicroCom
{
    public interface IMicroComExceptionCallback
    {
        void RaiseException(Exception e);
    }
}
