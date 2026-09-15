using System;
using System.Runtime.InteropServices;

namespace Avalonia.Win32.DirectX
{
    /// <summary>
    /// Tags a DXGI swap chain with a color space.
    /// </summary>
    /// <remarks>
    /// IDXGISwapChain3 is not described in directx.idl, so it is called through its vtable, the same
    /// way SwapChainGlSurface calls IDXGISwapChain2::SetMatrixTransform.
    /// </remarks>
    internal static unsafe class DxgiSwapChainColorSpace
    {
        private static readonly Guid s_swapChain3Guid = new("94d99bdb-f1f8-4ab0-b236-7da0170edab1");

        // Slots counted from the start of the vtable:
        // IUnknown(3) + IDXGIObject(4) + IDXGIDeviceSubObject(1) + IDXGISwapChain(10) +
        // IDXGISwapChain1(11) = 29, IDXGISwapChain2(7) ends at 35, so IDXGISwapChain3 begins at 36
        // with GetCurrentBackBufferIndex, followed by CheckColorSpaceSupport and SetColorSpace1.
        private const int CheckColorSpaceSupportSlot = 37;
        private const int SetColorSpace1Slot = 38;

        private const uint DXGI_SWAP_CHAIN_COLOR_SPACE_SUPPORT_FLAG_PRESENT = 1;

        /// <summary>
        /// Applies the color space if the swap chain supports presenting it, and reports whether it
        /// was really applied so the caller can fall back instead of presenting wrong colors.
        /// </summary>
        public static bool TryApply(IntPtr swapChain, DXGI_COLOR_SPACE_TYPE colorSpace)
        {
            if (swapChain == IntPtr.Zero)
                return false;

            var guid = s_swapChain3Guid;
            if (Marshal.QueryInterface(swapChain, in guid, out var swapChain3) != 0 || swapChain3 == IntPtr.Zero)
                return false;

            try
            {
                var vtable = *(IntPtr**)swapChain3;

                var checkColorSpaceSupport =
                    (delegate* unmanaged[Stdcall]<IntPtr, DXGI_COLOR_SPACE_TYPE, uint*, int>)
                    vtable[CheckColorSpaceSupportSlot];

                uint support;
                if (checkColorSpaceSupport(swapChain3, colorSpace, &support) != 0
                    || (support & DXGI_SWAP_CHAIN_COLOR_SPACE_SUPPORT_FLAG_PRESENT) == 0)
                {
                    return false;
                }

                var setColorSpace1 =
                    (delegate* unmanaged[Stdcall]<IntPtr, DXGI_COLOR_SPACE_TYPE, int>)vtable[SetColorSpace1Slot];

                return setColorSpace1(swapChain3, colorSpace) == 0;
            }
            finally
            {
                Marshal.Release(swapChain3);
            }
        }
    }
}
