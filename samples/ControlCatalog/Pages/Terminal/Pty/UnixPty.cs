using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace ControlCatalog.Pages.Terminal.Pty
{
    /// <summary>
    /// A shell on a Unix pseudo terminal (Linux and macOS).
    /// </summary>
    /// <remarks>
    /// The child is started with posix_spawn, not fork: a forked child of the .NET runtime would return to managed
    /// code with runtime state that belongs to threads that don't exist in the child. POSIX_SPAWN_SETSID makes the
    /// child a session leader, and its first open of the tty makes that tty its controlling terminal.
    /// On macOS an interactive zsh still ends up without a controlling terminal, and posix_spawn has no action for
    /// TIOCSCTTY, so a small Perl (or Python) wrapper calls it and then execs the shell.
    /// </remarks>
    internal sealed unsafe class UnixPty : IPty
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct WinSize
        {
            public ushort Rows, Columns, XPixels, YPixels;
        }

        private static readonly bool s_isMacOS = OperatingSystem.IsMacOS();
        private static readonly Libc s_libc = new();

        private int _master;
        // Closing the master fd while Write or Resize uses it would let them reach a reused descriptor
        private readonly object _masterLock = new();
        private int _slave;
        private readonly int _pid;
        private int _disposed;

        public UnixPty(int columns, int rows)
        {
            var shell = Environment.GetEnvironmentVariable("SHELL");
            if (string.IsNullOrEmpty(shell) || !System.IO.File.Exists(shell))
                shell = "/bin/sh";
            var term = PtyFactory.GetTerm();
            Description = $"{shell}, TERM={term}";

            int oRdwr = 2, oNoctty = s_isMacOS ? 0x20000 : 0x100, oCloexec = s_isMacOS ? 0x1000000 : 0x80000;

            _master = s_libc.posix_openpt(oRdwr | oNoctty);
            if (_master < 0)
                throw new InvalidOperationException($"posix_openpt failed: {Marshal.GetLastSystemError()}");
            SetCloseOnExec(_master);
            if (s_libc.grantpt(_master) != 0 || s_libc.unlockpt(_master) != 0)
                throw new InvalidOperationException($"grantpt/unlockpt failed: {Marshal.GetLastSystemError()}");
            var slavePath = Marshal.PtrToStringUTF8(s_libc.ptsname(_master))
                            ?? throw new InvalidOperationException("ptsname failed");

            // Keep the slave open in this process. Otherwise, on Linux, reading the master fails with EIO until the
            // child opens it. It is closed when the child exits, which ends the reader.
            var slavePathUtf8 = Marshal.StringToCoTaskMemUTF8(slavePath);
            try
            {
                _slave = s_libc.open((byte*)slavePathUtf8, oRdwr | oNoctty | oCloexec);
            }
            finally
            {
                Marshal.FreeCoTaskMem(slavePathUtf8);
            }
            if (_slave < 0)
                throw new InvalidOperationException($"open({slavePath}) failed: {Marshal.GetLastSystemError()}");

            Resize(columns, rows);
            _pid = Spawn(shell, slavePath, term);
        }

        public string Description { get; }

        private static int Spawn(string shell, string slavePath, string term)
        {
            const int posixSpawnSetSigDef = 0x04, posixSpawnSetSigMask = 0x08;
            var posixSpawnSetSid = s_isMacOS ? 0x0400 : 0x80;
            var posixSpawnCloexecDefault = s_isMacOS ? 0x4000 : 0;
            int sigKill = 9, sigStop = s_isMacOS ? 17 : 19;

            // The native structs are opaque and differ per platform, so allocate generously
            var fileActions = NativeMemory.AllocZeroed(1024);
            var attr = NativeMemory.AllocZeroed(1024);
            var sigDefault = NativeMemory.AllocZeroed(256);
            var sigMask = NativeMemory.AllocZeroed(256);
            var strings = new List<IntPtr>();
            try
            {
                s_libc.posix_spawn_file_actions_init(fileActions);
                s_libc.posix_spawnattr_init(attr);

                // stdin, stdout and stderr: the tty, opened after setsid so that it becomes the controlling terminal
                s_libc.posix_spawn_file_actions_addopen(fileActions, 0, Utf8(slavePath, strings), 2, 0);
                s_libc.posix_spawn_file_actions_adddup2(fileActions, 0, 1);
                s_libc.posix_spawn_file_actions_adddup2(fileActions, 0, 2);

                // The runtime changes signal dispositions and masks, and those survive exec
                s_libc.sigemptyset(sigDefault);
                for (var sig = 1; sig < 32; sig++)
                    if (sig != sigKill && sig != sigStop)
                        s_libc.sigaddset(sigDefault, sig);
                s_libc.sigemptyset(sigMask);
                s_libc.posix_spawnattr_setsigdefault(attr, sigDefault);
                s_libc.posix_spawnattr_setsigmask(attr, sigMask);
                s_libc.posix_spawnattr_setflags(attr,
                    (short)(posixSpawnSetSid | posixSpawnSetSigDef | posixSpawnSetSigMask | posixSpawnCloexecDefault));

                var (path, arguments) = GetCommand(shell);
                var argv = StringArray(arguments, strings);
                var envp = StringArray(BuildEnvironment(term), strings);

                int pid;
                var error = s_libc.posix_spawn(&pid, Utf8(path, strings), fileActions, attr, argv, envp);
                if (error != 0)
                    throw new InvalidOperationException($"posix_spawn({path}) failed: {error}");
                return pid;
            }
            finally
            {
                s_libc.posix_spawn_file_actions_destroy(fileActions);
                s_libc.posix_spawnattr_destroy(attr);
                NativeMemory.Free(fileActions);
                NativeMemory.Free(attr);
                NativeMemory.Free(sigDefault);
                NativeMemory.Free(sigMask);
                foreach (var s in strings)
                    Marshal.FreeCoTaskMem(s);
            }
        }

        /// <summary>
        /// Gets the executable and argv that run the shell with the tty as its controlling terminal.
        /// </summary>
        private static (string path, string[] argv) GetCommand(string shell)
        {
            if (!s_isMacOS)
                return (shell, new[] { shell });

            // 0x20007461 is TIOCSCTTY on macOS. Perl ships with macOS, the Python fallback with Xcode.
            if (System.IO.File.Exists("/usr/bin/perl"))
                return ("/usr/bin/perl", new[]
                {
                    "perl", "-e",
                    "ioctl(STDIN, 0x20007461, 0) or die \"TIOCSCTTY: $!\\n\"; exec { $ARGV[0] } @ARGV or die \"exec: $!\\n\"",
                    shell
                });
            return ("/usr/bin/python3", new[]
            {
                "python3", "-c",
                "import fcntl, os, sys, termios; fcntl.ioctl(0, termios.TIOCSCTTY, 0); os.execv(sys.argv[1], sys.argv[1:])",
                shell
            });
        }

        private static List<string> BuildEnvironment(string term)
        {
            var result = new List<string>();
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var name = (string)entry.Key;
                // ncurses prefers COLUMNS and LINES over the tty size, so programs would keep a stale size
                if (name is "TERM" or "COLUMNS" or "LINES" or "AVALONIA_TERMINAL_WRITE_LOG")
                    continue;
                result.Add($"{name}={entry.Value}");
            }
            result.Add("TERM=" + term);
            return result;
        }

        private static byte* Utf8(string s, List<IntPtr> strings)
        {
            var ptr = Marshal.StringToCoTaskMemUTF8(s);
            strings.Add(ptr);
            return (byte*)ptr;
        }

        private static byte** StringArray(IReadOnlyList<string> items, List<IntPtr> strings)
        {
            var array = (byte**)Marshal.AllocCoTaskMem(IntPtr.Size * (items.Count + 1));
            strings.Add((IntPtr)array);
            for (var i = 0; i < items.Count; i++)
                array[i] = Utf8(items[i], strings);
            array[items.Count] = null;
            return array;
        }

        private static void SetCloseOnExec(int fd)
        {
            const int fSetFd = 2, fdCloexec = 1;
            s_libc.fcntl(fd, fSetFd, fdCloexec);
        }

        public void Start(Action<byte[]> onOutput, Action<int?> onExit)
        {
            var reader = new Thread(() =>
            {
                const int eintr = 4;
                var buffer = new byte[16384];
                while (true)
                {
                    nint read;
                    fixed (byte* p = buffer)
                        read = s_libc.read(_master, p, buffer.Length);
                    if (read > 0)
                        onOutput(buffer.AsSpan(0, (int)read).ToArray());
                    // On Linux, a read fails with EIO when the slave side is closed
                    else if (read == 0 || Marshal.GetLastSystemError() != eintr)
                        break;
                }
                lock (_masterLock)
                {
                    s_libc.close(_master);
                    _master = -1;
                }
                onExit(WaitForExit());
            }) { IsBackground = true, Name = "PTY reader" };

            var waiter = new Thread(() =>
            {
                int status;
                while (s_libc.waitpid(_pid, &status, 0) < 0 && Marshal.GetLastSystemError() == 4)
                {
                }
                _exitStatus = status;
                _exited.Set();
                // Let the reader drain the output and see the end of it
                CloseSlave();
            }) { IsBackground = true, Name = "PTY waiter" };

            reader.Start();
            waiter.Start();
        }

        private int _exitStatus;
        private readonly ManualResetEventSlim _exited = new();

        private int? WaitForExit()
        {
            _exited.Wait();
            // WIFEXITED / WEXITSTATUS
            return (_exitStatus & 0x7F) == 0 ? (_exitStatus >> 8) & 0xFF : null;
        }

        private void CloseSlave()
        {
            var slave = Interlocked.Exchange(ref _slave, -1);
            if (slave >= 0)
                s_libc.close(slave);
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            if (_disposed != 0)
                return;
            PtyFactory.LogWrite(data);
            lock (_masterLock)
            fixed (byte* p = data)
            {
                if (_master < 0)
                    return;
                var offset = 0;
                while (offset < data.Length)
                {
                    var written = s_libc.write(_master, p + offset, data.Length - offset);
                    if (written <= 0)
                        return;
                    offset += (int)written;
                }
            }
        }

        public void Resize(int columns, int rows)
        {
            var size = new WinSize { Columns = (ushort)columns, Rows = (ushort)rows };
            lock (_masterLock)
            {
                if (_master >= 0)
                    s_libc.SetWindowSize(_master, &size);
            }
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            const int sigHup = 1;
            s_libc.kill(_pid, sigHup);
            CloseSlave();
        }

        /// <summary>
        /// libc entry points, bound with function pointers so that one class serves glibc and macOS.
        /// </summary>
        private sealed class Libc
        {
            private static readonly bool s_variadicOnStack =
                s_isMacOS && RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

            private readonly IntPtr _lib = NativeLibrary.Load(s_isMacOS ? "libSystem.dylib" : "libc.so.6");
            private readonly Dictionary<string, IntPtr> _exports = new();

            private IntPtr Get(string name)
            {
                lock (_exports)
                {
                    if (!_exports.TryGetValue(name, out var export))
                        _exports[name] = export = NativeLibrary.GetExport(_lib, name);
                    return export;
                }
            }

            public int posix_openpt(int flags) => ((delegate* unmanaged<int, int>)Get("posix_openpt"))(flags);
            public int grantpt(int fd) => ((delegate* unmanaged<int, int>)Get("grantpt"))(fd);
            public int unlockpt(int fd) => ((delegate* unmanaged<int, int>)Get("unlockpt"))(fd);
            public IntPtr ptsname(int fd) => ((delegate* unmanaged<int, IntPtr>)Get("ptsname"))(fd);

            // open is variadic, but calling it without the variadic argument is fine on every ABI
            public int open(byte* path, int flags) => ((delegate* unmanaged<byte*, int, int>)Get("open"))(path, flags);

            // Apple arm64 passes variadic arguments on the stack, so the 8 argument registers are filled first
            public int fcntl(int fd, int cmd, int arg) => s_variadicOnStack ?
                ((delegate* unmanaged<int, int, nint, nint, nint, nint, nint, nint, nint, int>)Get("fcntl"))(
                    fd, cmd, 0, 0, 0, 0, 0, 0, arg) :
                ((delegate* unmanaged<int, int, int, int>)Get("fcntl"))(fd, cmd, arg);

            public int SetWindowSize(int fd, WinSize* size)
            {
                nuint tiocswinsz = s_isMacOS ? 0x80087467 : 0x5414;
                return s_variadicOnStack ?
                    ((delegate* unmanaged<int, nuint, nint, nint, nint, nint, nint, nint, WinSize*, int>)Get("ioctl"))(
                        fd, tiocswinsz, 0, 0, 0, 0, 0, 0, size) :
                    ((delegate* unmanaged<int, nuint, WinSize*, int>)Get("ioctl"))(fd, tiocswinsz, size);
            }

            public nint read(int fd, byte* buffer, nint count) =>
                ((delegate* unmanaged<int, byte*, nint, nint>)Get("read"))(fd, buffer, count);
            public nint write(int fd, byte* buffer, nint count) =>
                ((delegate* unmanaged<int, byte*, nint, nint>)Get("write"))(fd, buffer, count);
            public int close(int fd) => ((delegate* unmanaged<int, int>)Get("close"))(fd);
            public int kill(int pid, int sig) => ((delegate* unmanaged<int, int, int>)Get("kill"))(pid, sig);
            public int waitpid(int pid, int* status, int options) =>
                ((delegate* unmanaged<int, int*, int, int>)Get("waitpid"))(pid, status, options);

            public int sigemptyset(void* set) => ((delegate* unmanaged<void*, int>)Get("sigemptyset"))(set);
            public int sigaddset(void* set, int sig) => ((delegate* unmanaged<void*, int, int>)Get("sigaddset"))(set, sig);

            public int posix_spawn_file_actions_init(void* fa) =>
                ((delegate* unmanaged<void*, int>)Get("posix_spawn_file_actions_init"))(fa);
            public int posix_spawn_file_actions_destroy(void* fa) =>
                ((delegate* unmanaged<void*, int>)Get("posix_spawn_file_actions_destroy"))(fa);
            public int posix_spawn_file_actions_addopen(void* fa, int fd, byte* path, int flags, int mode) =>
                ((delegate* unmanaged<void*, int, byte*, int, int, int>)Get("posix_spawn_file_actions_addopen"))(
                    fa, fd, path, flags, mode);
            public int posix_spawn_file_actions_adddup2(void* fa, int fd, int newFd) =>
                ((delegate* unmanaged<void*, int, int, int>)Get("posix_spawn_file_actions_adddup2"))(fa, fd, newFd);

            public int posix_spawnattr_init(void* attr) => ((delegate* unmanaged<void*, int>)Get("posix_spawnattr_init"))(attr);
            public int posix_spawnattr_destroy(void* attr) =>
                ((delegate* unmanaged<void*, int>)Get("posix_spawnattr_destroy"))(attr);
            public int posix_spawnattr_setflags(void* attr, short flags) =>
                ((delegate* unmanaged<void*, short, int>)Get("posix_spawnattr_setflags"))(attr, flags);
            public int posix_spawnattr_setsigdefault(void* attr, void* set) =>
                ((delegate* unmanaged<void*, void*, int>)Get("posix_spawnattr_setsigdefault"))(attr, set);
            public int posix_spawnattr_setsigmask(void* attr, void* set) =>
                ((delegate* unmanaged<void*, void*, int>)Get("posix_spawnattr_setsigmask"))(attr, set);

            public int posix_spawn(int* pid, byte* path, void* fileActions, void* attr, byte** argv, byte** envp) =>
                ((delegate* unmanaged<int*, byte*, void*, void*, byte**, byte**, int>)Get("posix_spawn"))(
                    pid, path, fileActions, attr, argv, envp);
        }
    }
}
