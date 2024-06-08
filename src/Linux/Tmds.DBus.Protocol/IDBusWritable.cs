namespace Tmds.DBus.Protocol;

public interface IDBusWritable
{
    void WriteTo(ref MessageWriter writer);
}
