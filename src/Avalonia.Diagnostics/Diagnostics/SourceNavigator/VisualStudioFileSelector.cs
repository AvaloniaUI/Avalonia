#nullable enable
using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Markup.Xaml.SourceInfo;
using System.Threading.Tasks;
using EnvDTE;
using System.Diagnostics;
using Process = System.Diagnostics.Process;
using Avalonia;
using Avalonia.Diagnostics;

namespace Avalonia.Diagnostics.SourceNavigator
{
    /// <summary>
    /// A utility class to determine a process parent on Windows.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessHelperWindows
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ProcessHelperWindows processInformation, int processInformationLength, out int returnLength);

        /// <summary>
        /// Gets the parent process of the current process.
        /// </summary>
        /// <returns>An instance of the Process class.</returns>
        public static Process? GetParentProcess()
        {
            return GetParentProcess(Process.GetCurrentProcess().Handle);
        }

        /// <summary>
        /// Gets the parent process of a specified process.
        /// </summary>
        /// <param name="handle">The process handle.</param>
        /// <returns>An instance of the Process class.</returns>
        public static Process? GetParentProcess(IntPtr handle)
        {
            var pbi = new ProcessHelperWindows();
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out _);
            if (status != 0)
                throw new System.ComponentModel.Win32Exception(status);

            try
            {
                return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }

    internal static class RetryHelper
    {
        public static void ExecuteWithRetry(Action action, int retries, int delayMs)
        {
            COMException? lastEx = null;
            for (int attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    action();
                    return; // Success
                }
                catch (COMException ex) when ((uint)ex.ErrorCode == 0x8001010A) // RPC_E_SERVERCALL_RETRYLATER
                {
                    lastEx = ex;
                    System.Threading.Thread.Sleep(delayMs);
                }
            }
            throw lastEx!;
        }
        public static T ExecuteWithRetry<T>(Func<T> action, int retries, int delayMs)
        {
            COMException? lastEx = null;
            for (int attempt = 1; attempt <= retries; attempt++)
            {
                try
                {
                    return action();
                }
                catch (COMException ex) when ((uint)ex.ErrorCode == 0x8001010A) // RPC_E_SERVERCALL_RETRYLATER
                {
                    lastEx = ex;
                    System.Threading.Thread.Sleep(delayMs);
                }
            }
            throw lastEx!;
        }

    }

    /// <summary>
    /// Provides an <see cref="IAvaloniaSourceNavigator"/> implementation
    /// that integrates with a running instance of Visual Studio via EnvDTE.
    /// </summary>
    /// <remarks>
    /// This navigator attempts to locate the current Visual Studio instance
    /// (either hosting the Avalonia Designer or debugging the application)
    /// and uses the <see cref="DTE"/> automation model to open and position
    /// the editor caret at the requested source location.
    /// </remarks>
    internal sealed class VisualStudioSourceNavigator : IAvaloniaSourceNavigator
    {
        private bool _isInitialized;
        private DTE? _cachedDte;

        /// <summary>
        /// Returns <c>true</c> if a Visual Studio instance is available for navigation.
        /// </summary>
        public bool CanNavigate()
        {
            EnsureInitialized();
            return _cachedDte != null;
        }

        /// <summary>
        /// Attempts to navigate to the specified file and position in Visual Studio.
        /// </summary>
        public Task NavigateToAsync(string filePath, int line, int column)
        {
            if (!CanNavigate() || string.IsNullOrEmpty(filePath))
                return Task.CompletedTask;

            try
            {
                var dte = _cachedDte!;
                RetryHelper.ExecuteWithRetry(() => dte.MainWindow.Activate(), 3, 150);

                dte.ItemOperations.OpenFile(filePath);

                var activeDocument = RetryHelper.ExecuteWithRetry(() => dte.ActiveDocument, 3, 150);
                if (activeDocument == null)
                    return Task.CompletedTask;

                var sel = RetryHelper.ExecuteWithRetry(() => (TextSelection)activeDocument.Selection, 3, 150);
                RetryHelper.ExecuteWithRetry(() => sel.MoveToLineAndOffset(line, column), 3, 150);
                RetryHelper.ExecuteWithRetry(() => sel.ActivePoint.TryToShow(), 3, 150);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avalonia] Failed to navigate in Visual Studio: {ex}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ensures a valid DTE instance has been resolved.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;
            _cachedDte ??= GetCurrentDte();
        }

        /// <summary>
        /// Attempts to locate the <see cref="EnvDTE.DTE"/> object for the
        /// Visual Studio instance that is either hosting the designer
        /// or debugging the current process.
        /// </summary>
        private DTE? GetCurrentDte()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return null;

            try
            {
                if (Design.IsDesignMode)
                {
                    var parentProcess = ProcessHelperWindows.GetParentProcess();
                    if (parentProcess?.ProcessName.Equals("devenv", StringComparison.OrdinalIgnoreCase) == true)
                    {
                        var rot = new RunningObjectTable();
                        return rot.FindDteByVisualStudioProcessId(parentProcess.Id);
                    }
                    return null;
                }
                else
                {
                    var processId = Process.GetCurrentProcess().Id;
                    var rot = new RunningObjectTable();
                    return rot.FindDteByDebuggedProcess(processId);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avalonia] Failed to obtain DTE: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Forces re-detection of the Visual Studio instance.
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
            _cachedDte = null;
        }

        private class RunningObjectTable
        {
            [DllImport("ole32.dll")] private static extern int GetRunningObjectTable(int reserved, out System.Runtime.InteropServices.ComTypes.IRunningObjectTable pprot);
            [DllImport("ole32.dll")] private static extern int CreateBindCtx(int reserved, out System.Runtime.InteropServices.ComTypes.IBindCtx ppbc);

            private readonly System.Runtime.InteropServices.ComTypes.IRunningObjectTable _rot;
            private readonly System.Runtime.InteropServices.ComTypes.IEnumMoniker _enum;

            public RunningObjectTable()
            {
                GetRunningObjectTable(0, out var rot);
                _rot = rot;
                _rot.EnumRunning(out _enum);
            }

            public DTE? FindDteByDebuggedProcess(int pid)
            {
                var monikers = new System.Runtime.InteropServices.ComTypes.IMoniker[1];
                while (_enum.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    _rot.GetObject(monikers[0], out var obj);
                    if (obj is DTE dte)
                    {
                        try
                        {
                            foreach (EnvDTE.Process p in dte.Debugger.DebuggedProcesses)
                            {
                                if (p.ProcessID == pid)
                                    return dte;
                            }
                        }
                        catch { /* ignore */ }
                    }
                }
                return null;
            }

            public DTE? FindDteByVisualStudioProcessId(int devenvPid)
            {
                var monikers = new System.Runtime.InteropServices.ComTypes.IMoniker[1];
                while (_enum.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    _rot.GetObject(monikers[0], out var obj);
                    if (obj is DTE dte)
                    {
                        try
                        {
                            var name = GetRotDisplayName(dte);
                            var pid = ExtractPidFromRotName(name);
                            if (pid == devenvPid)
                                return dte;
                        }
                        catch { /* ignore */ }
                    }
                }
                return null;
            }

            private static string GetRotDisplayName(DTE dte)
            {
                GetRunningObjectTable(0, out var rot);
                CreateBindCtx(0, out var ctx);
                rot.EnumRunning(out var enumMoniker);

                var monikers = new System.Runtime.InteropServices.ComTypes.IMoniker[1];
                while (enumMoniker.Next(1, monikers, IntPtr.Zero) == 0)
                {
                    rot.GetObject(monikers[0], out var obj);
                    if (ReferenceEquals(obj, dte))
                    {
                        monikers[0].GetDisplayName(ctx, null, out var name);
                        return name;
                    }
                }
                return string.Empty;
            }

            private static int ExtractPidFromRotName(string rotName)
            {
                var idx = rotName.LastIndexOf(':');
                if (idx >= 0 && int.TryParse(rotName.Substring(idx + 1), out var pid))
                    return pid;
                return -1;
            }
        }
    }
}
