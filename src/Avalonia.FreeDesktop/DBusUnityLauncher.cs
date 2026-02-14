using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Logging;
using Tmds.DBus.Protocol;
using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop
{
    internal class DBusUnityLauncher : ComCanonicalUnityLauncherEntryHandler
    {
        private readonly string _desktopUri;

        private DBusUnityLauncher(Connection connection, string desktopUri)
        {
            Connection = connection;
            _desktopUri = desktopUri;
        }

        public override Connection Connection { get; }

        public double LastProgress { get; private set; }

        public void SetProgress(double progress, bool visible)
        {
            LastProgress = progress;
            EmitUpdate(_desktopUri, new Dictionary<string, VariantValue>
            {
                ["progress"] = progress,
                ["progress-visible"] = visible,
            });
        }

        public static DBusUnityLauncher? TryCreate()
        {
            try
            {
                var connection = DBusHelper.DefaultConnection;
                if (connection is null)
                    return null;

                var desktopFile = DetectDesktopFile();
                var desktopUri = $"application://{desktopFile}";

                var appId = Path.GetFileNameWithoutExtension(desktopFile);
                var launcher = new DBusUnityLauncher(connection, desktopUri);
                var pathHandler = new PathHandler("/com/canonical/unity/launcherentry/" + appId.Replace('.', '/'));
                pathHandler.Add(launcher);
                connection.AddMethodHandler(pathHandler);

                return launcher;
            }
            catch (Exception e)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(null, "Unable to create Unity LauncherEntry: " + e);
                return null;
            }
        }

        private static string DetectDesktopFile()
        {
            // Check environment variables set by desktop environments when launching via .desktop file.
            var envFile = Environment.GetEnvironmentVariable("GIO_LAUNCHED_DESKTOP_FILE")
                ?? Environment.GetEnvironmentVariable("BAMF_DESKTOP_FILE_HINT");

            if (!string.IsNullOrEmpty(envFile))
            {
                var fileName = Path.GetFileName(envFile);
                Logger.TryGet(LogEventLevel.Debug, "DBUS")
                    ?.Log(null, "Using desktop file from environment: {File}", fileName);
                return fileName;
            }

            // Fallback: derive from assembly/process name.
            var appId = Assembly.GetEntryAssembly()?.GetName().Name
                ?? Process.GetCurrentProcess().ProcessName;
            var fallbackFile = $"{appId}.desktop";

            Logger.TryGet(LogEventLevel.Warning, "DBUS")
                ?.Log(null, "No desktop file hint found in environment; falling back to {File}. " +
                    "Set GIO_LAUNCHED_DESKTOP_FILE or ensure the .desktop filename matches the assembly name.",
                    fallbackFile);

            return fallbackFile;
        }
    }
}
