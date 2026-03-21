using System;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Avalonia.Platform.Storage.FileIO;

internal class BclLauncher : ILauncher
{
    public virtual Task<bool> LaunchUriAsync(Uri uri)
    {
        _ = uri ?? throw new ArgumentNullException(nameof(uri));
        if (uri.IsAbsoluteUri)
        {
            return Task.FromResult(Exec(uri.AbsoluteUri));
        }

        return Task.FromResult(false);
    }

    /// <summary>
    /// This Process based implementation doesn't handle the case, when there is no app to handle link.
    /// It will still return true in this case.
    /// </summary>
    public virtual Task<bool> LaunchFileAsync(IStorageItem storageItem)
    {
        _ = storageItem ?? throw new ArgumentNullException(nameof(storageItem));
        if (storageItem.TryGetLocalPath() is { } localPath
            && CanOpenFileOrDirectory(localPath))
        {
            return Task.FromResult(Exec(localPath));
        }

        return Task.FromResult(false);
    }

    protected virtual bool CanOpenFileOrDirectory(string localPath) => true;
    
    private static bool Exec(string urlOrFile)
    {
        if (OperatingSystem.IsLinux())
        {
            // If no associated application/json MimeType is found xdg-open opens return error
            // but it tries to open it anyway using the console editor (nano, vim, other..)
            var args = EscapeForShell(urlOrFile);
            ShellExecRaw($"xdg-open \\\"{args}\\\"", waitForExit: false);
            return true;
        }
        else if (OperatingSystem.IsWindows() || OperatingSystem.IsMacOS())
        {
            var info = new ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? urlOrFile : "open",
                CreateNoWindow = true,
                UseShellExecute = OperatingSystem.IsWindows()
            };
            // Using the argument list avoids having to escape spaces and other special 
            // characters that are part of valid macos file and folder paths.
            if (OperatingSystem.IsMacOS())
                info.ArgumentList.Add(urlOrFile);
            using var process = Process.Start(info);
            return true;
        }
        else
        {
            return false;
        }
    }
    
    private static string EscapeForShell(string input) => Regex
        .Replace(input, "(?=[`~!#&*()|;'<>])", "\\")
        .Replace("\"", "\\\\\\\"");
    
    private static void ShellExecRaw(string cmd, bool waitForExit = true)
    {
        using (var process = Process.Start(
                   new ProcessStartInfo
                   {
                       FileName = "/bin/sh",
                       Arguments = $"-c \"{cmd}\"",
                       RedirectStandardOutput = true,
                       UseShellExecute = false,
                       CreateNoWindow = true,
                       WindowStyle = ProcessWindowStyle.Hidden
                   }
               ))
        {
            if (waitForExit)
            {
                process?.WaitForExit();
            }
        }
    }
}
