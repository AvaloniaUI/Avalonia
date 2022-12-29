using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Platform;
using Avalonia.Logging;
using Avalonia.Platform;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop
{
    internal class DBusTrayIconImpl : ITrayIconImpl
    {
        private static int s_trayIconInstanceId;

        private readonly ObjectPath _dbusMenuPath;
        private readonly Connection? _connection;
        private readonly DBus? _dBus;

        private IDisposable? _serviceWatchDisposable;
        private StatusNotifierItemDbusObj? _statusNotifierItemDbusObj;
        private StatusNotifierWatcher? _statusNotifierWatcher;
        private (int, int, byte[]) _icon;

        private string? _sysTrayServiceName;
        private string? _tooltipText;
        private bool _isDisposed;
        private bool _serviceConnected;
        private bool _isVisible = true;

        public bool IsActive { get; private set; }
        public INativeMenuExporter? MenuExporter { get; }
        public Action? OnClicked { get; set; }
        public Func<IWindowIconImpl?, uint[]>? IconConverterDelegate { get; set; }

        public DBusTrayIconImpl()
        {
            _connection = DBusHelper.TryCreateNewConnection();

            if (_connection is null)
            {
                Logger.TryGet(LogEventLevel.Error, "DBUS")
                    ?.Log(this, "Unable to get a dbus connection for system tray icons.");

                return;
            }

            IsActive = true;

            _dBus = new DBusService(_connection, "org.freedesktop.DBus").CreateDBus("/org/freedesktop/DBus");
            _dbusMenuPath = DBusMenuExporter.GenerateDBusMenuObjPath;

            MenuExporter = DBusMenuExporter.TryCreateDetachedNativeMenu(_dbusMenuPath, _connection);

            WatchAsync();
        }

        private void InitializeSNWService()
        {
            if (_connection is null || _isDisposed)
                return;

            _statusNotifierWatcher = new StatusNotifierWatcherService(_connection, "org.kde.StatusNotifierWatcher")
                .CreateStatusNotifierWatcher("/StatusNotifierWatcher");
            _serviceConnected = true;
        }

        private async void WatchAsync()
        {
            var services = await _connection!.ListServicesAsync();
            if (!services.Contains("org.kde.StatusNotifierWatcher", StringComparer.Ordinal))
                return;

            _serviceWatchDisposable = await _dBus!.WatchNameOwnerChangedAsync((e, x) => { OnNameChange(x.A2); });
            var nameOwner = await _dBus.GetNameOwnerAsync("org.kde.StatusNotifierWatcher");
            OnNameChange(nameOwner);
        }

        private void OnNameChange(string? newOwner)
        {
            if (_isDisposed)
                return;

            if (!_serviceConnected & newOwner is not null)
            {
                _serviceConnected = true;
                InitializeSNWService();

                DestroyTrayIcon();

                if (_isVisible)
                    CreateTrayIcon();
            }
            else if (_serviceConnected & newOwner is null)
            {
                DestroyTrayIcon();
                _serviceConnected = false;
            }
        }

        private async void CreateTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed)
                return;

#if NET5_0_OR_GREATER
            var pid = Environment.ProcessId;
#else
            var pid = Process.GetCurrentProcess().Id;
#endif
            var tid = s_trayIconInstanceId++;

            _sysTrayServiceName = FormattableString.Invariant($"org.kde.StatusNotifierItem-{pid}-{tid}");
            _statusNotifierItemDbusObj = new StatusNotifierItemDbusObj(_connection, _dbusMenuPath);

            _connection.AddMethodHandler(_statusNotifierItemDbusObj);
            await _dBus!.RequestNameAsync(_sysTrayServiceName, 0);
            await _statusNotifierWatcher!.RegisterStatusNotifierItemAsync(_sysTrayServiceName);

            _statusNotifierItemDbusObj.SetTitleAndTooltip(_tooltipText);
            _statusNotifierItemDbusObj.SetIcon(_icon);
            _statusNotifierItemDbusObj.ActivationDelegate += OnClicked;
        }

        private void DestroyTrayIcon()
        {
            if (_connection is null || !_serviceConnected || _isDisposed || _statusNotifierItemDbusObj is null || _sysTrayServiceName is null)
                return;

            _dBus.ReleaseNameAsync(_sysTrayServiceName);
        }

        public void Dispose()
        {
            IsActive = false;
            _isDisposed = true;
            DestroyTrayIcon();
            _connection?.Dispose();
            _serviceWatchDisposable?.Dispose();
        }

        public void SetIcon(IWindowIconImpl? icon)
        {
            if (_isDisposed || IconConverterDelegate is null)
                return;

            if (icon is null)
            {
                _statusNotifierItemDbusObj?.SetIcon((1, 1, new byte[] { 255, 0, 0, 0 }));
                return;
            }

            var x11iconData = IconConverterDelegate(icon);

            if (x11iconData.Length == 0)
                return;

            var w = (int)x11iconData[0];
            var h = (int)x11iconData[1];

            var pixLength = w * h;
            var pixByteArrayCounter = 0;
            var pixByteArray = new byte[w * h * 4];

            for (var i = 0; i < pixLength; i++)
            {
                var rawPixel = x11iconData[i + 2];
                pixByteArray[pixByteArrayCounter++] = (byte)((rawPixel & 0xFF000000) >> 24);
                pixByteArray[pixByteArrayCounter++] = (byte)((rawPixel & 0xFF0000) >> 16);
                pixByteArray[pixByteArrayCounter++] = (byte)((rawPixel & 0xFF00) >> 8);
                pixByteArray[pixByteArrayCounter++] = (byte)(rawPixel & 0xFF);
            }

            _icon = (w, h, pixByteArray);
            _statusNotifierItemDbusObj?.SetIcon(_icon);
        }

        public void SetIsVisible(bool visible)
        {
            if (_isDisposed)
                return;

            switch (visible)
            {
                case true when !_isVisible:
                    DestroyTrayIcon();
                    CreateTrayIcon();
                    break;
                case false when _isVisible:
                    DestroyTrayIcon();
                    break;
            }

            _isVisible = visible;
        }

        public void SetToolTipText(string? text)
        {
            if (_isDisposed || text is null)
                return;
            _tooltipText = text;
            _statusNotifierItemDbusObj?.SetTitleAndTooltip(_tooltipText);
        }
    }

    /// <summary>
    /// DBus Object used for setting system tray icons.
    /// </summary>
    /// <remarks>
    /// Useful guide: https://web.archive.org/web/20210818173850/https://www.notmart.org/misc/statusnotifieritem/statusnotifieritem.html
    /// </remarks>
    internal class StatusNotifierItemDbusObj : IMethodHandler
    {
        private readonly Connection _connection;
        private readonly StatusNotifierItemProperties _backingProperties;

        public StatusNotifierItemDbusObj(Connection connection, ObjectPath dbusMenuPath)
        {
            _connection = connection;
            _backingProperties = new StatusNotifierItemProperties
            {
                Menu = dbusMenuPath, // Needs a dbus menu somehow
                ToolTip = (string.Empty, Array.Empty<(int, int, byte[])>(), string.Empty, string.Empty)
            };

            InvalidateAll();
        }

        public string Path => "/StatusNotifierItem";

        public event Action? ActivationDelegate;

        public void InvalidateAll()
        {
            EmitVoidSignal("NewTitle");
            EmitVoidSignal("NewIcon");
            EmitVoidSignal("NewAttentionIcon");
            EmitVoidSignal("NewOverlayIcon");
            EmitVoidSignal("NewToolTip");
            EmitStringSignal("NewStatus", _backingProperties.Status ?? string.Empty);
        }

        public void SetIcon((int, int, byte[]) dbusPixmap)
        {
            _backingProperties.IconPixmap = new[] { dbusPixmap };
            InvalidateAll();
        }

        public void SetTitleAndTooltip(string? text)
        {
            if (text is null)
                return;

            _backingProperties.Id = text;
            _backingProperties.Category = "ApplicationStatus";
            _backingProperties.Status = text;
            _backingProperties.Title = text;
            _backingProperties.ToolTip = (string.Empty, Array.Empty<(int, int, byte[])>(), text, string.Empty);
            InvalidateAll();
        }

        public bool RunMethodHandlerSynchronously(Message message) => false;

        public ValueTask HandleMethodAsync(MethodContext context)
        {
            switch (context.Request.InterfaceAsString)
            {
                case "org.kde.StatusNotifierItem":
                    switch (context.Request.MemberAsString, context.Request.SignatureAsString)
                    {
                        case ("ContextMenu", "ii"):
                            break;
                        case ("Activate", "ii"):
                            ActivationDelegate?.Invoke();
                            break;
                        case ("SecondaryActivate", "ii"):
                            break;
                        case ("Scroll", "is"):
                            break;
                    }

                    break;
                case "org.freedesktop.DBus.Properties":
                    switch (context.Request.MemberAsString, context.Request.SignatureAsString)
                    {
                        case ("Get", "ss"):
                        {
                            var reader = context.Request.GetBodyReader();
                            var interfaceName = reader.ReadString();
                            var member = reader.ReadString();
                            switch (member)
                            {
                                case "Category":
                                {
                                    using var writer = context.CreateReplyWriter("s");
                                    writer.WriteString(_backingProperties.Category);
                                    context.Reply(writer.CreateMessage());
                                    break;
                                }
                                case "Id":
                                {
                                    using var writer = context.CreateReplyWriter("s");
                                    writer.WriteString(_backingProperties.Id);
                                    context.Reply(writer.CreateMessage());
                                    break;
                                }
                                case "Title":
                                {
                                    using var writer = context.CreateReplyWriter("s");
                                    writer.WriteString(_backingProperties.Title);
                                    context.Reply(writer.CreateMessage());
                                    break;
                                }
                                case "Status":
                                {
                                    using var writer = context.CreateReplyWriter("s");
                                    writer.WriteString(_backingProperties.Status);
                                    context.Reply(writer.CreateMessage());
                                    break;
                                }
                                case "Menu":
                                {
                                    using var writer = context.CreateReplyWriter("o");
                                    writer.WriteObjectPath(_backingProperties.Menu);
                                    context.Reply(writer.CreateMessage());
                                    break;
                                }
                                case "IconPixmap":
                                {
                                    using var writer = context.CreateReplyWriter("a(iiay)");
                                    writer.WriteArray(_backingProperties.IconPixmap);
                                    context.Reply(writer.CreateMessage());
                                    break;
                                }
                                case "ToolTip":
                                {
                                    using var writer = context.CreateReplyWriter("(sa(iiay)ss)");
                                    writer.WriteStruct(_backingProperties.ToolTip);
                                    context.Reply(writer.CreateMessage());
                                    break;
                                }
                            }

                            break;
                        }
                        case ("GetAll", "s"):
                        {
                            var writer = context.CreateReplyWriter("a{sv}");
                            var dict = new Dictionary<string, object>
                            {
                                { "Category", _backingProperties.Category ?? string.Empty },
                                { "Id", _backingProperties.Id ?? string.Empty },
                                { "Title", _backingProperties.Title ?? string.Empty },
                                { "Status", _backingProperties.Status ?? string.Empty },
                                { "Menu", _backingProperties.Menu },
                                { "IconPixmap", _backingProperties.IconPixmap  },
                                { "ToolTip", _backingProperties.ToolTip }
                            };

                            writer.WriteDictionary(dict);
                            context.Reply(writer.CreateMessage());
                            break;
                        }
                    }

                    break;
            }

            return default;
        }

        private void EmitVoidSignal(string member)
        {
            using var writer = _connection.GetMessageWriter();
            writer.WriteSignalHeader(null, Path, "org.kde.StatusNotifierItem", member);
            _connection.TrySendMessage(writer.CreateMessage());
        }

        private void EmitStringSignal(string member, string value)
        {
            using var writer = _connection.GetMessageWriter();
            writer.WriteSignalHeader(null, Path, "org.kde.StatusNotifierItem", member, "s");
            writer.WriteString(value);
            _connection.TrySendMessage(writer.CreateMessage());
        }

        private record StatusNotifierItemProperties
        {
            public string? Category { get; set; }
            public string? Id { get; set; }
            public string? Title { get; set; }
            public string? Status { get; set; }
            public int WindowId { get; set; }
            public string? IconThemePath { get; set; }
            public ObjectPath Menu { get; set; }
            public bool ItemIsMenu { get; set; }
            public string? IconName { get; set; }
            public (int, int, byte[])[]? IconPixmap { get; set; }
            public string? OverlayIconName { get; set; }
            public (int, int, byte[])[]? OverlayIconPixmap { get; set; }
            public string? AttentionIconName { get; set; }
            public (int, int, byte[])[]? AttentionIconPixmap { get; set; }
            public string? AttentionMovieName { get; set; }
            public (string, (int, int, byte[])[], string, string) ToolTip { get; set; }
        }
    }
}
