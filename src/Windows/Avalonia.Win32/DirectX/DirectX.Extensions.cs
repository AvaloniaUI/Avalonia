using System;
using Avalonia.MicroCom;

namespace Avalonia.Win32.DirectX
{
    internal unsafe static class DirectXExtensions
    {
        public static T OpenSharedResource<T>(this ID3D11Device device, IntPtr handle)
        {
            var guid = MicroComRuntime.GetGuidFor(typeof(T));
            var pv = device.OpenSharedResource(handle, &guid);
            return MicroComRuntime.CreateProxyFor<T>(pv, true);
        }
    }
}
