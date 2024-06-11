using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.AtSpi;

public class CacheEntry
{
    public (string, ObjectPath) Accessible = (":0.0", "/org/a11y/atspi/accessible/object");
    public (string, ObjectPath) Application = (":0.0", "/org/a11y/atspi/accessible/application");
    public (string, ObjectPath) Parent = (":0.0", "/org/a11y/atspi/accessible/parent");
    public int IndexInParent = 0;
    public int ChildCount = 0;
    public string[] ApplicableInterfaces = ["org.a11y.atspi.Accessible"];
    public string LocalizedName = string.Empty;
    public AtSpiConstants.Role Role = default;
    public string RoleName = string.Empty;
    public uint[] ApplicableStates = [];

    public (
        (string, ObjectPath),
        (string, ObjectPath),
        (string, ObjectPath),
        int,
        int,
        string[],
        string,
        uint,
        string,
        uint[]) Convert() => (Accessible,
        Application,
        Parent,
        IndexInParent,
        ChildCount,
        ApplicableInterfaces,
        LocalizedName,
        (uint)Role,
        RoleName,
        ApplicableStates);
}
