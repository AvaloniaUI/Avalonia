using System;
using System.Collections;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ControlCatalog.Pages.Terminal.Pty
{
    /// <summary>
    /// A shell on a Windows pseudo console.
    /// </summary>
    /// <remarks>
    /// ConPTY is not a Unix tty: the child sees a normal console. ConPTY renders the console into its own xterm-like
    /// output, and turns the input that we write into console key events. TERM only affects what the program sends
    /// into ConPTY.
    /// </remarks>
    [SupportedOSPlatform("windows")]
    internal sealed unsafe class ConPty : IPty
    {
        private const uint ExtendedStartupInfoPresent = 0x00080000;
        private const uint CreateUnicodeEnvironment = 0x00000400;
        private const int StartfUseStdHandles = 0x00000100;
        private static readonly IntPtr s_procThreadAttributePseudoConsole = (IntPtr)0x00020016;

        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfo
        {
            public int cb;
            public IntPtr lpReserved, lpDesktop, lpTitle;
            public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags;
            public short wShowWindow, cbReserved2;
            public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct StartupInfoEx
        {
            public StartupInfo StartupInfo;
            public IntPtr lpAttributeList;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ProcessInformation
        {
            public IntPtr hProcess, hThread;
            public int dwProcessId, dwThreadId;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CreatePipe(out SafeFileHandle hReadPipe, out SafeFileHandle hWritePipe, IntPtr lpPipeAttributes, int nSize);

        [DllImport("kernel32.dll")]
        private static extern int CreatePseudoConsole(int size, SafeFileHandle hInput, SafeFileHandle hOutput, uint dwFlags, out IntPtr phPC);

        [DllImport("kernel32.dll")]
        private static extern int ResizePseudoConsole(IntPtr hPC, int size);

        [DllImport("kernel32.dll")]
        private static extern void ClosePseudoConsole(IntPtr hPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool InitializeProcThreadAttributeList(IntPtr lpAttributeList, int dwAttributeCount, int dwFlags, ref IntPtr lpSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool UpdateProcThreadAttribute(IntPtr lpAttributeList, uint dwFlags, IntPtr attribute, IntPtr lpValue, IntPtr cbSize, IntPtr lpPreviousValue, IntPtr lpReturnSize);

        [DllImport("kernel32.dll")]
        private static extern void DeleteProcThreadAttributeList(IntPtr lpAttributeList);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool CreateProcessW(string? lpApplicationName, IntPtr lpCommandLine, IntPtr lpProcessAttributes,
            IntPtr lpThreadAttributes, bool bInheritHandles, uint dwCreationFlags, IntPtr lpEnvironment,
            string? lpCurrentDirectory, ref StartupInfoEx lpStartupInfo, out ProcessInformation lpProcessInformation);

        [DllImport("kernel32.dll")]
        private static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

        [DllImport("kernel32.dll")]
        private static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr hObject);

        private readonly FileStream _input;
        private readonly FileStream _output;
        private readonly IntPtr _process;
        private IntPtr _pseudoConsole;
        private int _disposed;

        public ConPty(int columns, int rows)
        {
            var (shell, arguments, terminfo) = FindShell();
            var term = PtyFactory.GetTerm(terminfo is null ? Array.Empty<string>() : new[] { terminfo });
            Description = $"{shell}, TERM={term}";

            if (!CreatePipe(out var inputRead, out var inputWrite, IntPtr.Zero, 0)
                || !CreatePipe(out var outputRead, out var outputWrite, IntPtr.Zero, 0))
                throw new InvalidOperationException($"CreatePipe failed: {Marshal.GetLastPInvokeError()}");

            var hr = CreatePseudoConsole(Coord(columns, rows), inputRead, outputWrite, 0, out _pseudoConsole);
            if (hr != 0)
                throw new InvalidOperationException($"CreatePseudoConsole failed: 0x{hr:X8}");

            var attributeListSize = IntPtr.Zero;
            InitializeProcThreadAttributeList(IntPtr.Zero, 1, 0, ref attributeListSize);
            var attributeList = Marshal.AllocHGlobal(attributeListSize);
            var commandLine = Marshal.StringToHGlobalUni($"\"{shell}\" {arguments}");
            var environment = Marshal.StringToHGlobalUni(BuildEnvironment(term));
            try
            {
                if (!InitializeProcThreadAttributeList(attributeList, 1, 0, ref attributeListSize)
                    || !UpdateProcThreadAttribute(attributeList, 0, s_procThreadAttributePseudoConsole, _pseudoConsole,
                        (IntPtr)IntPtr.Size, IntPtr.Zero, IntPtr.Zero))
                    throw new InvalidOperationException($"Proc thread attributes failed: {Marshal.GetLastPInvokeError()}");

                var startupInfo = new StartupInfoEx { lpAttributeList = attributeList };
                startupInfo.StartupInfo.cb = sizeof(StartupInfoEx);
                // Null std handles: otherwise the child gets our redirected stdio (for example over ssh) instead of the
                // pseudo console
                startupInfo.StartupInfo.dwFlags = StartfUseStdHandles;

                if (!CreateProcessW(null, commandLine, IntPtr.Zero, IntPtr.Zero, false,
                        ExtendedStartupInfoPresent | CreateUnicodeEnvironment, environment, null, ref startupInfo,
                        out var processInformation))
                    throw new InvalidOperationException($"CreateProcess({shell}) failed: {Marshal.GetLastPInvokeError()}");

                CloseHandle(processInformation.hThread);
                _process = processInformation.hProcess;
            }
            finally
            {
                DeleteProcThreadAttributeList(attributeList);
                Marshal.FreeHGlobal(attributeList);
                Marshal.FreeHGlobal(commandLine);
                Marshal.FreeHGlobal(environment);
                // These ends belong to the pseudo console now
                inputRead.Dispose();
                outputWrite.Dispose();
            }

            _input = new FileStream(inputWrite, FileAccess.Write, 1);
            _output = new FileStream(outputRead, FileAccess.Read, 1);
        }

        public string Description { get; }

        private static int Coord(int columns, int rows) => (ushort)columns | ((ushort)rows << 16);

        /// <summary>
        /// Git Bash if it is installed, otherwise cmd.exe. PATH is not searched, because System32\bash.exe is WSL.
        /// </summary>
        private static (string shell, string arguments, string? terminfo) FindShell()
        {
            var roots = new[]
            {
                Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\GitForWindows")?.GetValue("InstallPath") as string,
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Git")
            };
            foreach (var root in roots)
            {
                if (root is null)
                    continue;
                var bash = Path.Combine(root, "bin", "bash.exe");
                if (File.Exists(bash))
                    return (bash, "--login -i", Path.Combine(root, "usr", "share", "terminfo"));
            }

            return (Path.Combine(Environment.SystemDirectory, "cmd.exe"), "", null);
        }

        private static string BuildEnvironment(string term)
        {
            var sb = new StringBuilder();
            foreach (DictionaryEntry entry in Environment.GetEnvironmentVariables())
            {
                var name = (string)entry.Key;
                if (name.Equals("TERM", StringComparison.OrdinalIgnoreCase)
                    || name.Equals("AVALONIA_TERMINAL_WRITE_LOG", StringComparison.OrdinalIgnoreCase))
                    continue;
                sb.Append(name).Append('=').Append(entry.Value).Append('\0');
            }
            sb.Append("TERM=").Append(term).Append('\0').Append('\0');
            return sb.ToString();
        }

        public void Start(Action<byte[]> onOutput, Action<int?> onExit)
        {
            new Thread(() =>
            {
                var buffer = new byte[16384];
                try
                {
                    int read;
                    while ((read = _output.Read(buffer, 0, buffer.Length)) > 0)
                        onOutput(buffer.AsSpan(0, read).ToArray());
                }
                catch (IOException)
                {
                }
                catch (ObjectDisposedException)
                {
                }
                onExit(GetExitCodeProcess(_process, out var code) ? (int)code : null);
                CloseHandle(_process);
            }) { IsBackground = true, Name = "ConPTY reader" }.Start();

            new Thread(() =>
            {
                WaitForSingleObject(_process, uint.MaxValue);
                // The pseudo console keeps the output pipe open after the shell exits
                ClosePseudoConsoleOnce();
            }) { IsBackground = true, Name = "ConPTY waiter" }.Start();
        }

        public void Write(ReadOnlySpan<byte> data)
        {
            if (_disposed != 0)
                return;
            PtyFactory.LogWrite(data);
            try
            {
                _input.Write(data);
                _input.Flush();
            }
            catch (IOException)
            {
            }
        }

        public void Resize(int columns, int rows)
        {
            if (_pseudoConsole != IntPtr.Zero)
                ResizePseudoConsole(_pseudoConsole, Coord(columns, rows));
        }

        private void ClosePseudoConsoleOnce()
        {
            var pc = Interlocked.Exchange(ref _pseudoConsole, IntPtr.Zero);
            // ClosePseudoConsole can block until the output is drained, which the reader thread does
            if (pc != IntPtr.Zero)
                ClosePseudoConsole(pc);
        }

        public void Dispose()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
                return;
            _input.Dispose();
            Task.Run(ClosePseudoConsoleOnce);
        }
    }
}
