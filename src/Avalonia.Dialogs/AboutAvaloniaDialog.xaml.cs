using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Avalonia.Dialogs
{
    public class AboutAvaloniaDialog : Window
    {
        private static readonly Version s_version = typeof(AboutAvaloniaDialog).Assembly.GetName().Version;

        public static string Version { get; } = $@"v{s_version.ToString(2)}";

        public static bool IsDevelopmentBuild { get; } = s_version.Revision == 999;

        public static string Copyright { get; } = $"© {DateTime.Now.Year} The Avalonia Project";

        public AboutAvaloniaDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = this;
        }

        

        private static void ShellExec(string cmd, bool waitForExit = true)
        {
            var escapedArgs = Regex.Replace(cmd, "(?=[`~!#&*()|;'<>])", "\\")
                .Replace("\"", "\\\\\\\"");

            using (var process = Process.Start(
                new ProcessStartInfo
                {
                    FileName = "/bin/sh",
                    Arguments = $"-c \"{escapedArgs}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                }
            ))
            {
                if (waitForExit)
                {
                    process.WaitForExit();
                }
            }
        }

        private void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var url = "https://www.avaloniaui.net/";
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // If no associated application/json MimeType is found xdg-open opens retrun error
                // but it tries to open it anyway using the console editor (nano, vim, other..)
                ShellExec($"xdg-open {url}", waitForExit: false);
            }
            else
            {
                using Process process = Process.Start(new ProcessStartInfo
                {
                    FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? url : "open",
                    Arguments = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? $"{url}" : "",
                    CreateNoWindow = true,
                    UseShellExecute = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                });
            }
        }
    }
}
