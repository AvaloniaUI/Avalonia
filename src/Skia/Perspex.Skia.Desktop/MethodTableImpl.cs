using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

/* OBSOLETE

//Library loaders were taken from https://github.com/kekekeks/evhttp-sharp/tree/master/EvHttpSharp/Interop (MIT licensed)

namespace Perspex.Skia
{
    class MethodTableImpl : MethodTable
    {
        public MethodTableImpl() : base(GetMethodTable())
        {
        }

        class DetectedPlatformInfo
        {
            public DetectedPlatformInfo(IDynLoader loader, string name, string cpuArch, string dllExtension)
            {
                Loader = loader;
                Name = name;
                CpuArch = cpuArch;
                DllExtension = dllExtension;
            }

            public IDynLoader Loader { get; }
            public string Name { get; }
            public string CpuArch { get;}
            public string DllExtension { get; }
        }

        static DetectedPlatformInfo DetectPlatform()
        {
            var macInfo = new DetectedPlatformInfo(new OsxLoader(), "Darwin", IntPtr.Size == 4 ? "i686" : "x86_64", ".dylib");
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Unix:
                    // Well, there are chances MacOSX is reported as Unix instead of MacOSX.
                    // Instead of platform check, we'll do a feature checks (Mac specific root folders)
                    if (Directory.Exists("/Applications")
                        & Directory.Exists("/System")
                        & Directory.Exists("/Users")
                        & Directory.Exists("/Volumes"))
                        return macInfo;
                    else
                    {
                        string cpuArch;
                        using (var proc =
                            Process.Start(
                                (new ProcessStartInfo("uname", "-p")
                                {
                                    UseShellExecute = false,
                                    RedirectStandardOutput = true
                                })))
                        {
                            proc.WaitForExit();
                            cpuArch = proc.StandardOutput.ReadToEnd().Trim();
                        }
                        return new DetectedPlatformInfo(new LinuxLoader(), "Linux", cpuArch, ".so");
                    }

                case PlatformID.MacOSX:
                    return macInfo;

                default:
                    return new DetectedPlatformInfo(new Win32Loader(), "Windows", IntPtr.Size == 4 ? "i686" : "x86_64", ".dll");
            }
        }

        static IEnumerable<string> GeneratePossiblePaths()
        {
            foreach (
                var basePath in
                    new[] {Assembly.GetEntryAssembly(), typeof (MethodTable).Assembly}.Select(
                        a => Path.GetDirectoryName(a?.GetModules()?[0]?.FullyQualifiedName))
                        .Concat(new[] { Directory.GetCurrentDirectory()}))
            {
                if(basePath == null)
                    continue;
                yield return Path.Combine(basePath);
                yield return Path.Combine(basePath, "native");
                yield return Path.Combine(basePath, "lib");
                yield return Path.Combine(basePath, "lib", "native");
            }
        }

        static IntPtr GetMethodTable()
        {
            var plat = DetectPlatform();

            var name = "libperspesk" + plat.DllExtension;
            var paths = GeneratePossiblePaths().Select(p => Path.Combine(p, plat.Name, plat.CpuArch, name)).ToList();
            var dll = paths.FirstOrDefault(File.Exists);
            var msg = "Unable to find native library in following paths: '" +
                      string.Join("', '", paths) + "'";
            Console.Error.WriteLine(msg);
            if (dll == null)
                throw new FileNotFoundException(msg);
            IntPtr hLib = plat.Loader.LoadLibrary(dll);
            IntPtr pGetMethodTable = plat.Loader.GetProcAddress(hLib, "GetPerspexMethodTable");
            var getTable = (GetPerspexMethodTableDelegate)
                Marshal.GetDelegateForFunctionPointer(pGetMethodTable, typeof (GetPerspexMethodTableDelegate));
            return getTable();
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr GetPerspexMethodTableDelegate();


        internal interface IDynLoader
        {
            IntPtr LoadLibrary(string dll);
            IntPtr GetProcAddress(IntPtr dll, string proc);

        }

        class LinuxLoader : IDynLoader
        {
            // ReSharper disable InconsistentNaming
            [DllImport("libdl.so.2")]
            private static extern IntPtr dlopen(string path, int flags);

            [DllImport("libdl.so.2")]
            private static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("libdl.so.2")]
            private static extern IntPtr dlerror();

            // ReSharper restore InconsistentNaming

            static string DlError()
            {
                return Marshal.PtrToStringAuto(dlerror());
            }

            public IntPtr LoadLibrary(string dll)
            {
                var handle = dlopen(dll, 1);
                if (handle == IntPtr.Zero)
                    throw new Win32Exception(DlError());
                return handle;
            }

            public IntPtr GetProcAddress(IntPtr dll, string proc)
            {
                var ptr = dlsym(dll, proc);
                if (ptr == IntPtr.Zero)
                    throw new Win32Exception(DlError());
                return ptr;
            }
        }

        internal class Win32Loader : IDynLoader
        {
            [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
            private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

            [DllImport("kernel32", EntryPoint = "LoadLibraryW", SetLastError = true, CharSet = CharSet.Unicode)]
            private static extern IntPtr LoadLibrary(string lpszLib);

            [DllImport("kernel32", EntryPoint = "SetDllDirectoryW", SetLastError = true, CharSet = CharSet.Unicode)]
            extern static bool SetDllDirectory(string lpPathName);

            [DllImport("kernel32", EntryPoint = "GetDllDirectoryW", SetLastError = true, CharSet = CharSet.Unicode)]
            extern static int GetDllDirectory(int nBufferLength, char[] lpBuffer);
            
            IntPtr IDynLoader.LoadLibrary(string dll)
            {
                var buffer = new char[2048];
                int oldLen = GetDllDirectory(buffer.Length, buffer);
                var oldDir = new string(buffer, 0, oldLen);

                SetDllDirectory(Path.GetDirectoryName(dll));

                var handle = LoadLibrary(dll);
                if (handle == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                SetDllDirectory(oldDir);

                return handle;
            }

            IntPtr IDynLoader.GetProcAddress(IntPtr dll, string proc)
            {
                var ptr = GetProcAddress(dll, proc);
                if (ptr == IntPtr.Zero)
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return ptr;
            }
        }

        class OsxLoader : IDynLoader
        {
            // ReSharper disable InconsistentNaming
            [DllImport("/usr/lib/libSystem.dylib")]
            public static extern IntPtr dlopen(string path, int mode);

            [DllImport("/usr/lib/libSystem.dylib")]
            private static extern IntPtr dlsym(IntPtr handle, string symbol);

            [DllImport("/usr/lib/libSystem.dylib")]
            private static extern IntPtr dlerror();

            // ReSharper restore InconsistentNaming

            static string DlError()
            {
                return Marshal.PtrToStringAuto(dlerror());
            }

            public IntPtr LoadLibrary(string dll)
            {
                var handle = dlopen(dll, 1);
                if (handle == IntPtr.Zero)
                    throw new Win32Exception(DlError());
                return handle;
            }

            public IntPtr GetProcAddress(IntPtr dll, string proc)
            {
                var ptr = dlsym(dll, proc);
                if (ptr == IntPtr.Zero)
                    throw new Win32Exception(DlError());
                return ptr;
            }
        }
    }
}

	*/