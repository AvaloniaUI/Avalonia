using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                var appId = Assembly.GetEntryAssembly()?.GetName().Name
                    ?? Process.GetCurrentProcess().ProcessName;
                var desktopUri = $"application://{appId}.desktop";

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
    }
}
