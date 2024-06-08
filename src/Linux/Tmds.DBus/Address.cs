// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace Tmds.DBus
{
    /// <summary>
    /// Helper class for determining D-Bus addresses.
    /// </summary>
    public static class Address
    {
        private static bool _systemAddressResolved = false;
        private static string _systemAddress = null;
        private static bool _sessionAddressResolved = false;
        private static string _sessionAddress = null;

        /// <summary>
        /// Address of System bus.
        /// </summary>
        public static string System
        {
            get
            {
                if (_systemAddressResolved)
                {
                    return _systemAddress;
                }
                
                _systemAddress = GetEnvironmentVariable("DBUS_SYSTEM_BUS_ADDRESS");
                if (String.IsNullOrEmpty(_systemAddress) && !Environment.IsWindows)
                    _systemAddress = "unix:path=/var/run/dbus/system_bus_socket";
                
                _systemAddressResolved = true;
                return _systemAddress;
            }
        }

        /// <summary>
        /// Address of Session bus.
        /// </summary>
        public static string Session
        {
            get
            {
                if (_sessionAddressResolved)
                {
                    return _sessionAddress;
                }
                _sessionAddress = GetEnvironmentVariable("DBUS_SESSION_BUS_ADDRESS");
                
                if (string.IsNullOrEmpty(_sessionAddress))
                {
                    if (Environment.IsWindows)
                    {
                        _sessionAddress = GetSessionBusAddressFromSharedMemory();
                    }
                    else
                    {
                        _sessionAddress = GetSessionBusAddressFromX11();
                    }
                }
                _sessionAddressResolved = true;
                return _sessionAddress;
            }
        }

        private static string GetEnvironmentVariable(string name)
        {
            return global::System.Environment.GetEnvironmentVariable(name);
        }
        
        private static string GetSessionBusAddressFromX11()
        {
            if (!string.IsNullOrEmpty(GetEnvironmentVariable("DISPLAY")))
            {
                var display = Interop.XOpenDisplay(null);
                if (display == IntPtr.Zero)
                {
                    return null;
                }
                string username;
                unsafe
                {
                    const int BufLen = 1024;
                    byte* stackBuf = stackalloc byte[BufLen];
                    Interop.Passwd passwd;
                    IntPtr result;
                    Interop.getpwuid_r(Interop.getuid(), out passwd, stackBuf, BufLen, out result);
                    if (result != IntPtr.Zero)
                    {
                        username = Marshal.PtrToStringAnsi(passwd.Name);
                    }
                    else
                    {
                        return null;
                    }
                }
                var machineId = Environment.MachineId.Replace("-", string.Empty);
                var selectionName = $"_DBUS_SESSION_BUS_SELECTION_{username}_{machineId}";
                var selectionAtom = Interop.XInternAtom(display, selectionName, false);
                if (selectionAtom == IntPtr.Zero)
                {
                    return null;
                }
                var owner = Interop.XGetSelectionOwner(display, selectionAtom);
                if (owner == IntPtr.Zero)
                {
                    return null;
                }
                var addressAtom = Interop.XInternAtom(display, "_DBUS_SESSION_BUS_ADDRESS", false);
                if (addressAtom == IntPtr.Zero)
                {
                    return null;
                }
                
                IntPtr actualReturnType;
                IntPtr actualReturnFormat;
                IntPtr nrItemsReturned;
                IntPtr bytesAfterReturn;
                IntPtr propReturn;
                int rv = Interop.XGetWindowProperty(display, owner, addressAtom, 0, 1024, false, (IntPtr)31 /* XA_STRING */,
                    out actualReturnType, out actualReturnFormat, out nrItemsReturned, out bytesAfterReturn, out propReturn);
                string address = rv == 0 ? Marshal.PtrToStringAnsi(propReturn) : null;
                if (propReturn != IntPtr.Zero)
                {
                    Interop.XFree(propReturn);
                }
                
                Interop.XCloseDisplay(display);
    
                return address;
            }
            else
            {
                return null;
            }
        }

#if NET6_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        private static string GetSessionBusAddressFromSharedMemory()
        {
            string result = ReadSharedMemoryString("DBusDaemonAddressInfo", 255);
            if (String.IsNullOrEmpty(result))
                result = ReadSharedMemoryString("DBusDaemonAddressInfoDebug", 255); // a DEBUG build of the daemon uses this different address...            
            return result;
        }

#if NET6_0_OR_GREATER
        [SupportedOSPlatform("windows")]
#endif
        private static string ReadSharedMemoryString(string id, long maxlen = -1)
        {
            MemoryMappedFile shmem;
            try
            {
                shmem = MemoryMappedFile.OpenExisting(id);
            }
            catch
            {
                shmem = null;
            }
            if (shmem == null)
                return null;
            MemoryMappedViewStream s = shmem.CreateViewStream();
            long len = s.Length;
            if (maxlen >= 0 && len > maxlen)
                len = maxlen;
            if (len == 0)
                return string.Empty;
            if (len > Int32.MaxValue)
                len = Int32.MaxValue;
            byte[] bytes = new byte[len];
            int count = s.Read(bytes, 0, (int)len);
            if (count <= 0)
                return string.Empty;

            count = 0;
            while (count < len && bytes[count] != 0)
                count++;

            return global::System.Text.Encoding.UTF8.GetString(bytes, 0, count);
        }
    }
}
