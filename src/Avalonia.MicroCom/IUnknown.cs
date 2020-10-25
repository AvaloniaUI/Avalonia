using System;

namespace Avalonia.MicroCom
{
    public interface IUnknown : IDisposable
    {
        void AddRef();
        void Release();
        int QueryInterface(Guid guid, out IntPtr ppv);
        T QueryInterface<T>() where T : IUnknown;
    }
}
