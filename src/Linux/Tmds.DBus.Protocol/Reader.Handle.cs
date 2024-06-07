namespace Tmds.DBus.Protocol;

public ref partial struct Reader
{
    public T? ReadHandle<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>() where T : SafeHandle
        => ReadHandleGeneric<T>();

    internal T? ReadHandleGeneric<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]T>()
    {
        int idx = (int)ReadUInt32();
        if (idx >= _handleCount)
        {
            throw new IndexOutOfRangeException();
        }
        if (_handles is not null)
        {
            return _handles.ReadHandleGeneric<T>(idx);
        }
        return default(T);
    }

    // note: The handle is still owned (i.e. Disposed) by the Message.
    public IntPtr ReadHandleRaw()
    {
        int idx = (int)ReadUInt32();
        if (idx >= _handleCount)
        {
            throw new IndexOutOfRangeException();
        }
        if (_handles is not null)
        {
            return _handles.ReadHandleRaw(idx);
        }
        return new IntPtr(-1);
    }
}
