namespace Tmds.DBus.Protocol;

public ref partial struct MessageWriter
{
    public void WriteHandle(SafeHandle value)
    {
        int idx = HandleCount;
        AddHandle(value);
        WriteInt32(idx);
    }

    public void WriteVariantHandle(SafeHandle value)
    {
        WriteSignature(ProtocolConstants.UnixFdSignature);
        WriteHandle(value);
    }

    private int HandleCount => _handles?.Count ?? 0;

    private void AddHandle(SafeHandle handle)
    {
        if (_handles is null)
        {
            _handles = new(isRawHandleCollection: false);
        }
        _handles.AddHandle(handle);
    }
}
