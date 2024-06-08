// Copyright 2006 Alp Toker <alp@atoker.com>
// Copyright 2016 Tom Deseyn <tom.deseyn@gmail.com>
// This software is made available under the MIT License
// See COPYING for details

using System;
using System.IO;
using System.Runtime.InteropServices;
#if NET6_0_OR_GREATER
using System.Runtime.Versioning;
#endif
using Tmds.DBus.Protocol;

namespace Tmds.DBus
{
    internal static class Environment
    {
        private const string MachineUuidPath = @"/var/lib/dbus/machine-id";

        public static readonly EndianFlag NativeEndianness;

#if NET6_0_OR_GREATER
        [SupportedOSPlatformGuard("windows")]
#endif
        public static readonly bool IsWindows;

        static Environment()
        {
            NativeEndianness = BitConverter.IsLittleEndian ? EndianFlag.Little : EndianFlag.Big;
            IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        }

        private static string _machineId;
        public static string MachineId
        {
            get
            {
                if (_machineId != null)
                {
                    return _machineId;
                }
                if (File.Exists(MachineUuidPath))
                {
                    _machineId = Guid.Parse(File.ReadAllText(MachineUuidPath).Substring(0, 32)).ToString("N");
                }
                else
                {
                    _machineId = Guid.Empty.ToString("N");
                }
                return _machineId;
            }
        }

        private static string _uid;
        public static string UserId
        {
            get
            {
                if (_uid != null)
                {
                    return _uid;
                }

                if (Environment.IsWindows)
                {
                    _uid = System.Security.Principal.WindowsIdentity.GetCurrent().User.Value;
                }
                else
                {
                    _uid = Interop.geteuid().ToString();
                }

                return _uid;
            }
        }
    }
}