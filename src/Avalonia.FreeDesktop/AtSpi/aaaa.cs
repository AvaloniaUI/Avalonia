using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Tmds.DBus.Protocol;

#pragma warning disable
#nullable enable
namespace Tmds.DBus.SourceGenerator
{
    internal abstract class OrgA11yAtspiAccessible : IDBusInterfaceHandler
    {
        private readonly SynchronizationContext? _synchronizationContext;
        public OrgA11yAtspiAccessible(bool emitOnCapturedContext = true)
        {
            if (emitOnCapturedContext)
                _synchronizationContext = SynchronizationContext.Current;
        }

        public PathHandler? PathHandler { get; set; }
        public abstract Connection Connection { get; }
        public string InterfaceName { get; } = "org.a11y.atspi.Accessible";
        public string? Name { get; set; }
        public string? Description { get; set; }
        public (string? , ObjectPath) Parent { get; set; }
        public int ChildCount { get; set; }
        public string? Locale { get; set; }
        public string? AccessibleId { get; set; }

        public void ReplyGetProperty(string name, MethodContext context)
        {
            Debug.Write(this.GetType().ToString());
            Debug.Write(" - ");
            Debug.Write(Name);
            Debug.Write(" - ");
            Debug.Write(name);
            switch (name)
            {
                case "Name":
                {
                    MessageWriter writer = context.CreateReplyWriter("v");
                    writer.WriteSignature("s");
                    writer.WriteNullableString(Name);
                    context.Reply(writer.CreateMessage());
                    writer.Dispose();
                    break;
                }

                case "Description":
                {
                    MessageWriter writer = context.CreateReplyWriter("v");
                    writer.WriteSignature("s");
                    writer.WriteNullableString(Description);
                    context.Reply(writer.CreateMessage());
                    writer.Dispose();
                    break;
                }

                case "Parent":
                {
                    MessageWriter writer = context.CreateReplyWriter("v");
                    writer.WriteSignature("(so)");
                    writer.WriteStruct_rsoz(Parent);
                    context.Reply(writer.CreateMessage());
                    writer.Dispose();
                    Debug.Write(Parent);
                    break;
                }

                case "ChildCount":
                {
                    MessageWriter writer = context.CreateReplyWriter("v");
                    writer.WriteSignature("i");
                    writer.WriteInt32(ChildCount);
                    context.Reply(writer.CreateMessage());
                    writer.Dispose();
                    Debug.Write(ChildCount);
                    break;
                }

                case "Locale":
                {
                    MessageWriter writer = context.CreateReplyWriter("v");
                    writer.WriteSignature("s");
                    writer.WriteNullableString(Locale);
                    context.Reply(writer.CreateMessage());
                    writer.Dispose();
                    break;
                }

                case "AccessibleId":
                {
                    MessageWriter writer = context.CreateReplyWriter("v");
                    writer.WriteSignature("s");
                    writer.WriteNullableString(AccessibleId);
                    context.Reply(writer.CreateMessage());
                    writer.Dispose();
                    break;
                }
            }
            Debug.WriteLine("");
        }

        public void ReplyGetAllProperties(MethodContext context)
        {
            Reply();
            void Reply()
            {
                MessageWriter writer = context.CreateReplyWriter("a{sv}");
                ArrayStart dictStart = writer.WriteDictionaryStart();
                writer.WriteDictionaryEntryStart();
                writer.WriteString("Name");
                writer.WriteSignature("s");
                writer.WriteNullableString(Name);
                writer.WriteDictionaryEntryStart();
                writer.WriteString("Description");
                writer.WriteSignature("s");
                writer.WriteNullableString(Description);
                writer.WriteDictionaryEntryStart();
                writer.WriteString("Parent");
                writer.WriteSignature("(so)");
                writer.WriteStruct_rsoz(Parent);
                writer.WriteDictionaryEntryStart();
                writer.WriteString("ChildCount");
                writer.WriteSignature("i");
                writer.WriteInt32(ChildCount);
                writer.WriteDictionaryEntryStart();
                writer.WriteString("Locale");
                writer.WriteSignature("s");
                writer.WriteNullableString(Locale);
                writer.WriteDictionaryEntryStart();
                writer.WriteString("AccessibleId");
                writer.WriteSignature("s");
                writer.WriteNullableString(AccessibleId);
                writer.WriteDictionaryEnd(dictStart);
                context.Reply(writer.CreateMessage());
            }
        }

        public ReadOnlyMemory<byte> IntrospectXml { get; } = "<interface xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" name=\"org.a11y.atspi.Accessible\">\n  <method name=\"GetChildAtIndex\">\n    <arg name=\"index\" type=\"i\" direction=\"in\" />\n    <arg type=\"(so)\" direction=\"out\" />\n  </method>\n  <method name=\"GetChildren\">\n    <arg type=\"a(so)\" direction=\"out\" />\n  </method>\n  <method name=\"GetIndexInParent\">\n    <arg type=\"i\" direction=\"out\" />\n  </method>\n  <method name=\"GetRelationSet\">\n    <arg type=\"a(ua(so))\" direction=\"out\" />\n  </method>\n  <method name=\"GetRole\">\n    <arg type=\"u\" direction=\"out\" />\n  </method>\n  <method name=\"GetRoleName\">\n    <arg type=\"s\" direction=\"out\" />\n  </method>\n  <method name=\"GetLocalizedRoleName\">\n    <arg type=\"s\" direction=\"out\" />\n  </method>\n  <method name=\"GetState\">\n    <arg type=\"au\" direction=\"out\" />\n  </method>\n  <method name=\"GetAttributes\">\n    <arg type=\"a{ss}\" direction=\"out\" />\n  </method>\n  <method name=\"GetApplication\">\n    <arg type=\"(so)\" direction=\"out\" />\n  </method>\n  <method name=\"GetInterfaces\">\n    <arg type=\"as\" direction=\"out\" />\n  </method>\n  <property name=\"Name\" type=\"s\" access=\"read\" />\n  <property name=\"Description\" type=\"s\" access=\"read\" />\n  <property name=\"Parent\" type=\"(so)\" access=\"read\" />\n  <property name=\"ChildCount\" type=\"i\" access=\"read\" />\n  <property name=\"Locale\" type=\"s\" access=\"read\" />\n  <property name=\"AccessibleId\" type=\"s\" access=\"read\" />\n</interface>"u8.ToArray();

        protected abstract ValueTask<(string, ObjectPath)> OnGetChildAtIndexAsync(int index);
        protected abstract ValueTask<(string, ObjectPath)[]> OnGetChildrenAsync();
        protected abstract ValueTask<int> OnGetIndexInParentAsync();
        protected abstract ValueTask<(uint, (string, ObjectPath)[])[]> OnGetRelationSetAsync();
        protected abstract ValueTask<uint> OnGetRoleAsync();
        protected abstract ValueTask<string> OnGetRoleNameAsync();
        protected abstract ValueTask<string> OnGetLocalizedRoleNameAsync();
        protected abstract ValueTask<uint[]> OnGetStateAsync();
        protected abstract ValueTask<Dictionary<string, string>> OnGetAttributesAsync();
        protected abstract ValueTask<(string, ObjectPath)> OnGetApplicationAsync();
        protected abstract ValueTask<string[]> OnGetInterfacesAsync();
        public async ValueTask ReplyInterfaceRequest(MethodContext context)
        {
            switch (context.Request.MemberAsString, context.Request.SignatureAsString)
            {
                case ("GetChildAtIndex", "i"):
                {
                    int index;
                    ReadParameters();
                    void ReadParameters()
                    {
                        Reader reader = context.Request.GetBodyReader();
                        index = reader.ReadInt32();
                    }

                    (string, ObjectPath) ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<(string, ObjectPath)> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                (string, ObjectPath) ret1 = await OnGetChildAtIndexAsync(index);
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetChildAtIndexAsync(index);
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("(so)");
                        writer.WriteStruct_rsoz(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetChildren", "" or null):
                {
                    (string, ObjectPath)[] ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<(string, ObjectPath)[]> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                (string, ObjectPath)[] ret1 = await OnGetChildrenAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetChildrenAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("a(so)");
                        writer.WriteArray_arsoz(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetIndexInParent", "" or null):
                {
                    int ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<int> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                int ret1 = await OnGetIndexInParentAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetIndexInParentAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("i");
                        writer.WriteInt32(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetRelationSet", "" or null):
                {
                    (uint, (string, ObjectPath)[])[] ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<(uint, (string, ObjectPath)[])[]> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                (uint, (string, ObjectPath)[])[] ret1 = await OnGetRelationSetAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetRelationSetAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("a(ua(so))");
                        writer.WriteArray_aruarsozz(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetRole", "" or null):
                {
                    uint ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<uint> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                uint ret1 = await OnGetRoleAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetRoleAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("u");
                        writer.WriteUInt32(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetRoleName", "" or null):
                {
                    string ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<string> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                string ret1 = await OnGetRoleNameAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetRoleNameAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("s");
                        writer.WriteNullableString(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetLocalizedRoleName", "" or null):
                {
                    string ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<string> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                string ret1 = await OnGetLocalizedRoleNameAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetLocalizedRoleNameAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("s");
                        writer.WriteNullableString(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetState", "" or null):
                {
                    uint[] ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<uint[]> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                uint[] ret1 = await OnGetStateAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetStateAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("au");
                        writer.WriteArray_au(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetAttributes", "" or null):
                {
                    Dictionary<string, string> ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<Dictionary<string, string>> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                Dictionary<string, string> ret1 = await OnGetAttributesAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetAttributesAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("a{ss}");
                        writer.WriteDictionary_aess(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetApplication", "" or null):
                {
                    (string, ObjectPath) ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<(string, ObjectPath)> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                (string, ObjectPath) ret1 = await OnGetApplicationAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetApplicationAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("(so)");
                        writer.WriteStruct_rsoz(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }

                case ("GetInterfaces", "" or null):
                {
                    string[] ret;
                    if (_synchronizationContext is not null)
                    {
                        TaskCompletionSource<string[]> tsc = new();
                        _synchronizationContext.Post(async _ =>
                        {
                            try
                            {
                                string[] ret1 = await OnGetInterfacesAsync();
                                tsc.SetResult(ret1);
                            }
                            catch (Exception e)
                            {
                                tsc.SetException(e);
                            }
                        }, null);
                        ret = await tsc.Task;
                    }
                    else
                    {
                        ret = await OnGetInterfacesAsync();
                    }

                    if (!context.NoReplyExpected)
                        Reply();
                    void Reply()
                    {
                        MessageWriter writer = context.CreateReplyWriter("as");
                        writer.WriteArray_as(ret);
                        context.Reply(writer.CreateMessage());
                        writer.Dispose();
                    }

                    break;
                }
            }
        }
    }
}
