using System;
using System.IO;

namespace ControlCatalog.Pages.Terminal.Pty
{
    /// <summary>
    /// A shell process that runs on a pseudo terminal.
    /// </summary>
    internal interface IPty : IDisposable
    {
        /// <summary>
        /// A short description of the process, for example the shell path and TERM.
        /// </summary>
        string Description { get; }

        /// <summary>
        /// Starts reading the output on a background thread. <paramref name="onOutput"/> and
        /// <paramref name="onExit"/> are called on that thread.
        /// </summary>
        void Start(Action<byte[]> onOutput, Action<int?> onExit);

        void Write(ReadOnlySpan<byte> data);

        void Resize(int columns, int rows);
    }

    internal static class PtyFactory
    {
        public static bool IsSupported =>
            OperatingSystem.IsWindows() || OperatingSystem.IsLinux() || OperatingSystem.IsMacOS();

        public static IPty Create(int columns, int rows)
        {
            if (OperatingSystem.IsWindows())
                return new ConPty(columns, rows);
            return new UnixPty(columns, rows);
        }

        /// <summary>
        /// Picks TERM=ansi-mini, or ansi if the terminfo database has no ansi-mini entry.
        /// </summary>
        public static string GetTerm(params string[] extraTerminfoDirectories)
        {
            const string term = "ansi-mini";
            var directories = new[]
            {
                Environment.GetEnvironmentVariable("TERMINFO"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".terminfo"),
                "/etc/terminfo",
                "/lib/terminfo",
                "/usr/share/terminfo",
                "/usr/lib/terminfo"
            };

            foreach (var dir in extraTerminfoDirectories)
                if (Exists(dir))
                    return term;
            foreach (var dir in directories)
                if (Exists(dir))
                    return term;
            return "ansi";

            // ncurses uses both the first letter and its hex code as the subdirectory
            static bool Exists(string? dir) =>
                !string.IsNullOrEmpty(dir)
                && (File.Exists(Path.Combine(dir, "a", term)) || File.Exists(Path.Combine(dir, "61", term)));
        }

        /// <summary>
        /// Writes every chunk sent to the program as hex, when AVALONIA_TERMINAL_WRITE_LOG names a file.
        /// </summary>
        public static void LogWrite(ReadOnlySpan<byte> data)
        {
            var path = Environment.GetEnvironmentVariable("AVALONIA_TERMINAL_WRITE_LOG");
            if (string.IsNullOrEmpty(path))
                return;
            try
            {
                File.AppendAllText(path, Convert.ToHexString(data).ToLowerInvariant() + Environment.NewLine);
            }
            catch (IOException)
            {
            }
        }
    }
}
