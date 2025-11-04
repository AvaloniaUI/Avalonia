using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using Avalonia.Markup.Xaml.SourceInfo;
using System.Threading.Tasks;

namespace Avalonia.Diagnostics.SourceNavigator
{
    /// <summary>
    /// Provides an <see cref="IAvaloniaSourceNavigator"/> implementation
    /// that integrates with JetBrains Rider.
    /// </summary>
    /// <remarks>
    /// This navigator detects a running Rider process and attempts
    /// to open the requested file at the specified line and column
    /// using Rider’s command-line arguments.
    /// </remarks>
    internal sealed class RiderSourceNavigator : IAvaloniaSourceNavigator
    {
        private string _riderFilePath = "";
        private bool _isInitialized;

        /// <summary>
        /// Determines whether Rider is currently available for navigation.
        /// </summary>
        public bool CanNavigate()
        {
            EnsureInitialized();
            return !string.IsNullOrEmpty(_riderFilePath);
        }

        /// <summary>
        /// Attempts to open the specified file and position in Rider.
        /// </summary>
        public Task NavigateToAsync(string filePath, int line, int column)
        {
            if (!CanNavigate() || string.IsNullOrEmpty(filePath))
                return Task.CompletedTask;

            try
            {
                int adjustedColumn = CalculateVisualColumn(filePath, line, column);

                Process.Start(new ProcessStartInfo
                {
                    FileName = _riderFilePath!,
                    Arguments = $"--line {line} --column {adjustedColumn} \"{filePath}\"",
                    UseShellExecute = false
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avalonia] Failed to navigate in Rider: {ex}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Ensures Rider’s executable path has been detected.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_isInitialized)
                return;

            _isInitialized = true;

            try
            {
                _riderFilePath = Process.GetProcessesByName("rider64")
                    .Select(c => c.MainModule?.FileName)
                    .Concat(Process.GetProcessesByName("rider")
                    .Select(c => c.MainModule?.FileName))
                    .FirstOrDefault() ?? "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Avalonia] Rider detection failed: {ex}");
                _riderFilePath = "";
            }
        }

        /// <summary>
        /// Calculates the visual column offset for accurate positioning when tabs are present.
        /// </summary>
        private static int CalculateVisualColumn(string filePath, int line, int column, int tabSize = 4)
        {
            if (!File.Exists(filePath))
                return column;

            string? textLine = File.ReadLines(filePath).Skip(line - 1).FirstOrDefault();
            if (textLine == null)
                return column;

            int visualColumn = 0;
            for (int i = 0; i < Math.Min(column - 1, textLine.Length); i++)
            {
                visualColumn += textLine[i] == '\t' ? tabSize : 1;
            }

            return visualColumn;
        }

        /// <summary>
        /// Forces Rider process re-detection. Mainly useful for tests or when Rider restarts.
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
            _riderFilePath = "";
        }
    }
}
