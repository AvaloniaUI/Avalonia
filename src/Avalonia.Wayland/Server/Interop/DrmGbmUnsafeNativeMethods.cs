using System;
using System.Runtime.InteropServices;

namespace Avalonia.Wayland.Server.Interop
{
    [Flags]
    internal enum GbmBoFlags
    {
        GBM_BO_USE_SCANOUT = (1 << 0),
        GBM_BO_USE_RENDERING = (1 << 2),
        GBM_BO_USE_LINEAR = (1 << 4),
    }

    [StructLayout(LayoutKind.Explicit)]
    internal struct GbmBoHandle
    {
        [FieldOffset(0)]
        public IntPtr Ptr;
        [FieldOffset(0)]
        public int S32;
        [FieldOffset(0)]
        public uint U32;
        [FieldOffset(0)]
        public long S64;
        [FieldOffset(0)]
        public ulong U64;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct DrmDevice
    {
        public IntPtr* Nodes; // char** — DRM_NODE_MAX sized array
        public int AvailableNodes; // DRM_NODE_* bitmask
        public int BusType;
        public IntPtr BusInfo; // union of pointers
        public IntPtr DeviceInfo; // union of pointers
    }

    internal static unsafe class DrmGbmUnsafeNativeMethods
    {
        private const string LibGbm = "libgbm.so.1";
        private const string LibDrm = "libdrm.so.2";
        private const string LibC = "libc";

        // DRM format constants
        public static uint DrmFormatCode(char a, char b, char c, char d) =>
            (uint)a | ((uint)b << 8) | ((uint)c << 16) | ((uint)d << 24);

        public static readonly uint DRM_FORMAT_ARGB8888 = DrmFormatCode('A', 'R', '2', '4');
        public static readonly uint DRM_FORMAT_XRGB8888 = DrmFormatCode('X', 'R', '2', '4');
        public const ulong DRM_FORMAT_MOD_INVALID = 0x00ffffffffffffffUL;

        // GBM device
        [DllImport(LibGbm)]
        public static extern IntPtr gbm_create_device(int fd);

        [DllImport(LibGbm)]
        public static extern void gbm_device_destroy(IntPtr gbm);

        // GBM buffer object - basic
        [DllImport(LibGbm)]
        public static extern IntPtr gbm_bo_create(IntPtr gbm, uint width, uint height,
            uint format, GbmBoFlags flags);

        [DllImport(LibGbm)]
        public static extern IntPtr gbm_bo_create_with_modifiers2(IntPtr gbm, uint width, uint height,
            uint format, ulong* modifiers, uint count, GbmBoFlags flags);

        [DllImport(LibGbm)]
        public static extern void gbm_bo_destroy(IntPtr bo);

        // GBM buffer object - properties
        [DllImport(LibGbm)]
        public static extern uint gbm_bo_get_width(IntPtr bo);

        [DllImport(LibGbm)]
        public static extern uint gbm_bo_get_height(IntPtr bo);

        [DllImport(LibGbm)]
        public static extern uint gbm_bo_get_format(IntPtr bo);

        [DllImport(LibGbm)]
        public static extern ulong gbm_bo_get_modifier(IntPtr bo);

        [DllImport(LibGbm)]
        public static extern int gbm_bo_get_plane_count(IntPtr bo);

        [DllImport(LibGbm)]
        public static extern GbmBoHandle gbm_bo_get_handle_for_plane(IntPtr bo, int plane);

        [DllImport(LibGbm)]
        public static extern uint gbm_bo_get_stride_for_plane(IntPtr bo, int plane);

        [DllImport(LibGbm)]
        public static extern uint gbm_bo_get_offset(IntPtr bo, int plane);

        // DRM
        [DllImport(LibDrm)]
        public static extern int drmPrimeHandleToFD(int fd, uint handle, uint flags, out int prime_fd);

        [DllImport(LibDrm)]
        public static extern int drmGetDeviceFromDevId(ulong devId, uint flags, out DrmDevice* device);

        [DllImport(LibDrm)]
        public static extern void drmFreeDevice(DrmDevice** device);

        public const int DRM_NODE_RENDER = 2;

        // libc
        [DllImport(LibC)]
        public static extern int open(string pathname, int flags);

        [DllImport(LibC)]
        public static extern int close(int fd);

        public const int O_RDWR = 2;
    }
}
