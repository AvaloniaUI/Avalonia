namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    private ReadOnlySpan<byte> IntrospectionHeader =>
        """
        <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
        <node>

        """u8;

    private ReadOnlySpan<byte> IntrospectionFooter =>
        """
        </node>

        """u8;

    private ReadOnlySpan<byte> NodeNameStart =>
        """
        <node name="
        """u8;

    private ReadOnlySpan<byte> NodeNameEnd =>
        """
        "/>

        """u8;

    public void WriteIntrospectionXml(scoped ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls, IEnumerable<string> childNames)
        => WriteIntrospectionXml(interfaceXmls, baseInterfaceXmls: default, childNames: default,
            childNamesEnumerable: childNames ?? throw new ArgumentNullException(nameof(childNames)));

    internal void WriteIntrospectionXml(
        scoped ReadOnlySpan<ReadOnlyMemory<byte>> interfaceXmls,
        scoped ReadOnlySpan<ReadOnlyMemory<byte>> baseInterfaceXmls,
        scoped ReadOnlySpan<string> childNames,
        IEnumerable<string>? childNamesEnumerable)
    {
        WritePadding(DBusType.UInt32);
        Span<byte> lengthSpan = GetSpan(4);
        Advance(4);

        int bytesWritten = 0;
        bytesWritten += WriteRaw(IntrospectionHeader);
        foreach (var xml in interfaceXmls)
        {
            bytesWritten += WriteRaw(xml.Span);
        }
        foreach (var xml in baseInterfaceXmls)
        {
            bytesWritten += WriteRaw(xml.Span);
        }
        // D-Bus names only consist of '[A-Z][a-z][0-9]_'.
        // We don't need to any escaping for use as an XML attribute value.
        foreach (var childName in childNames)
        {
            bytesWritten += WriteRaw(NodeNameStart);
            bytesWritten += WriteRaw(childName);
            bytesWritten += WriteRaw(NodeNameEnd);
        }
        if (childNamesEnumerable is not null)
        {
            foreach (var childName in childNamesEnumerable)
            {
                bytesWritten += WriteRaw(NodeNameStart);
                bytesWritten += WriteRaw(childName);
                bytesWritten += WriteRaw(NodeNameEnd);
            }
        }
        bytesWritten += WriteRaw(IntrospectionFooter);

        Unsafe.WriteUnaligned<uint>(ref MemoryMarshal.GetReference(lengthSpan), (uint)bytesWritten);
        WriteByte(0);
    }
}
