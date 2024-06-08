using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Shapes;
using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.Automation;

internal class MethodHandlerMultiplexer(string rootPath) : IMethodHandler
{
    private Dictionary<string, IMethodHandler> _handlers = new();

    public void AddHandler(string @interface, IMethodHandler handler)
    {
        _handlers.Add(@interface, handler);
    }

    public string Path { get; } = rootPath;

    private ReadOnlyMemory<byte> _introspectXml =
        "<node name=\"/org/a11y/atspi/accessible/root\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" >\n  <interface name=\"org.freedesktop.DBus.Properties\">\n    <method name=\"Get\">\n      <arg type=\"s\" name=\"interface_name\" direction=\"in\"/>\n      <arg type=\"s\" name=\"property_name\" direction=\"in\"/>\n      <arg type=\"v\" name=\"value\" direction=\"out\"/>\n    </method>\n    <method name=\"GetAll\">\n      <arg type=\"s\" name=\"interface_name\" direction=\"in\"/>\n      <arg type=\"a{sv}\" name=\"properties\" direction=\"out\"/>\n    </method>\n    <method name=\"Set\">\n      <arg type=\"s\" name=\"interface_name\" direction=\"in\"/>\n      <arg type=\"s\" name=\"property_name\" direction=\"in\"/>\n      <arg type=\"v\" name=\"value\" direction=\"in\"/>\n    </method>\n    <signal name=\"PropertiesChanged\">\n      <arg type=\"s\" name=\"interface_name\"/>\n      <arg type=\"a{sv}\" name=\"changed_properties\"/>\n      <arg type=\"as\" name=\"invalidated_properties\"/>\n    </signal>\n  </interface>\n  <interface name=\"org.freedesktop.DBus.Introspectable\">\n    <method name=\"Introspect\">\n      <arg type=\"s\" name=\"xml_data\" direction=\"out\"/>\n    </method>\n  </interface>\n  <interface name=\"org.freedesktop.DBus.Peer\">\n    <method name=\"Ping\"/>\n    <method name=\"GetMachineId\">\n      <arg type=\"s\" name=\"machine_uuid\" direction=\"out\"/>\n    </method>\n  </interface>\n  <interface name=\"org.a11y.atspi.Application\">\n    <method name=\"GetLocale\">\n      <arg type=\"u\" name=\"lctype\" direction=\"in\"/>\n      <arg type=\"s\" name=\"unnamed_arg1\" direction=\"out\"/>\n    </method>\n    <method name=\"RegisterEventListener\">\n      <arg type=\"s\" name=\"event\" direction=\"in\"/>\n    </method>\n    <method name=\"DeregisterEventListener\">\n      <arg type=\"s\" name=\"event\" direction=\"in\"/>\n    </method>\n    <property type=\"s\" name=\"ToolkitName\" access=\"read\"/>\n    <property type=\"s\" name=\"Version\" access=\"read\"/>\n    <property type=\"s\" name=\"AtspiVersion\" access=\"read\"/>\n    <property type=\"i\" name=\"Id\" access=\"readwrite\"/>\n  </interface>\n  <interface name=\"org.a11y.atspi.Accessible\">\n    <method name=\"GetChildAtIndex\">\n      <annotation name=\"org.qtproject.QtDBus.QtTypeName.Out0\"\n                  value=\"QSpiObjectReference\"/>\n      <arg type=\"i\" name=\"index\" direction=\"in\"/>\n      <arg type=\"(so)\" name=\"unnamed_arg1\" direction=\"out\"/>\n    </method>\n    <method name=\"GetChildren\">\n      <annotation name=\"org.qtproject.QtDBus.QtTypeName.Out0\"\n                  value=\"QSpiObjectReferenceArray\"/>\n      <arg type=\"a(so)\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetIndexInParent\">\n      <arg type=\"i\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetRelationSet\">\n      <annotation name=\"org.qtproject.QtDBus.QtTypeName.Out0\"\n value=\"QSpiRelationArray\"/>\n      <arg type=\"a(ua(so))\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetRole\">\n      <arg type=\"u\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetRoleName\">\n      <arg type=\"s\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetLocalizedRoleName\">\n      <arg type=\"s\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetState\">\n      <annotation name=\"org.qtproject.QtDBus.QtTypeName.Out0\"\n                  value=\"QSpiIntList\"/>\n      <arg type=\"au\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetAttributes\">\n      <annotation name=\"org.qtproject.QtDBus.QtTypeName.Out0\"\n                  value=\"QSpiAttributeSet\"/>\n      <arg type=\"a{ss}\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetApplication\">\n      <annotation name=\"org.qtproject.QtDBus.QtTypeName.Out0\"\n                  value=\"QSpiObjectReference\"/>\n      <arg type=\"(so)\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <method name=\"GetInterfaces\">\n      <arg type=\"as\" name=\"unnamed_arg0\" direction=\"out\"/>\n    </method>\n    <property type=\"s\" name=\"Name\" access=\"read\"/>\n    <property type=\"s\" name=\"Description\" access=\"read\"/>\n    <property type=\"(so)\" name=\"Parent\" access=\"read\">\n      <annotation name=\"org.qtproject.QtDBus.QtTypeName\"\n                  value=\"QSpiObjectReference\"/>\n    </property>\n    <property type=\"i\" name=\"ChildCount\" access=\"read\"/>\n    <property type=\"s\" name=\"Locale\" access=\"read\"/>\n    <property type=\"s\" name=\"AccessibleId\" access=\"read\"/>\n  </interface>\n</node>"u8
            .ToArray();

    public async ValueTask HandleMethodAsync(MethodContext context)
    {
        var reqInt = context.Request.InterfaceAsString;
        switch (reqInt)
        {
            case "org.freedesktop.DBus.Properties":
                switch (context.Request.MemberAsString)
                {
                    case ("PropertiesChanged"):
                    case ("GetAll"):
                    case ("Get"):
                    case ("Set"):
                        Reply();

                        void Reply()
                        {
                            Reader reader = context.Request.GetBodyReader();
                            string intfc = reader.ReadString(); 
                            
                            if (_handlers.TryGetValue(intfc, out var m2))
                                m2.HandleMethodAsync(context);

                        }
                        break;
 
                }

                break;

            case "org.freedesktop.DBus.Introspectable":

                switch (context.Request.MemberAsString, context.Request.SignatureAsString)
                {
                    case ("Introspect", "" or null):
                    {
                        context.ReplyIntrospectXml(new[] { _introspectXml });
                        break;
                    }
                }

                break;
            default:

                if (_handlers.TryGetValue(reqInt, out var matchHandler))
                    matchHandler.HandleMethodAsync(context); 

                break;
        }
    }

    public bool RunMethodHandlerSynchronously(Message message) => true;
}
