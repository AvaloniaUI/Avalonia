using Tmds.DBus.SourceGenerator;

namespace Avalonia.FreeDesktop
{
    [DBusInterface("./DBusXml/DBus.xml")]
    [DBusInterface("./DBusXml/StatusNotifierWatcher.xml")]
    [DBusInterface("./DBusXml/com.canonical.AppMenu.Registrar.xml")]
    [DBusInterface("./DBusXml/org.fcitx.Fcitx.InputContext.xml")]
    [DBusInterface("./DBusXml/org.fcitx.Fcitx.InputMethod.xml")]
    [DBusInterface("./DBusXml/org.fcitx.Fcitx.InputContext1.xml")]
    [DBusInterface("./DBusXml/org.fcitx.Fcitx.InputMethod1.xml")]
    [DBusInterface("./DBusXml/org.freedesktop.IBus.Portal.xml")]
    [DBusInterface("./DBusXml/org.freedesktop.portal.FileChooser.xml")]
    [DBusInterface("./DBusXml/org.freedesktop.portal.Request.xml")]
    [DBusInterface("./DBusXml/org.freedesktop.portal.Settings.xml")]
    [DBusHandler("./DBusXml/DBusMenu.xml")]
    [DBusHandler("./DBusXml/StatusNotifierItem.xml")]
    internal class DBusInterfaces { }
}
