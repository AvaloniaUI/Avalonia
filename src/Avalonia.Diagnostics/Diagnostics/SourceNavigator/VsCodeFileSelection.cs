using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml.SourceInfo;

namespace Avalonia.Diagnostics.SourceNavigator
{
    /// <summary>
    /// Provides an <see cref="IAvaloniaSourceNavigator"/> implementation
    /// that integrates with Visual Studio Code.
    /// </summary>
    /// <remarks>
    /// This navigator detects a running VS Code or VS Code Insiders process
    /// and opens the requested file using the VS Code CLI (`-g file:line:column`).
    /// </remarks>
    internal sealed class VsCodeSourceNavigator : IAvaloniaSourceNavigator
    {
        private bool _isInitialized;
        private string _vscodePath = "";

        /// <summary>
        /// Determines whether VS Code is currently available for navigation.
        /// </summary>
        public bool CanNavigate()
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(_vscodePath);
        }

        /// <summary>
        /// Opens the specified file at the given line and column in VS Code.
        /// </summary>
        public Task NavigateToAsync(string filePath, int line, int column)
        {
            if (!CanNavigate() || string.IsNullOrEmpty(filePath))
                return Task.CompletedTask;

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = _vscodePath,
                    Arguments = $"-g \"{filePath}:{line}:{column}\"",
                    UseShellExecute = false
                };

                // VS Code sometimes sets ELECTRON_RUN_AS_NODE in its child processes;
                // remove it to avoid unexpected parsing errors when starting new instances.
                psi.EnvironmentVariables.Remove("ELECTRON_RUN_AS_NODE");

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avalonia] Failed to navigate in VS Code: {ex}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ensures the VS Code executable path has been detected.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            try
            {
                _vscodePath = Process.GetProcessesByName("Code")
                    .Concat(Process.GetProcessesByName("Code - Insiders"))
                    .Select(c => c.MainModule?.FileName)
                    .FirstOrDefault() ?? "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avalonia] VS Code detection failed: {ex}");
                _vscodePath = "";
            }
        }

        /// <summary>
        /// Forces re-detection of the VS Code process path.
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
            _vscodePath = "";
        }
    }
}
