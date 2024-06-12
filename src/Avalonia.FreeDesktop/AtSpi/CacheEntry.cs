using Tmds.DBus.Protocol;

namespace Avalonia.FreeDesktop.AtSpi;

public class CacheEntry
{
    public (string, ObjectPath) Accessible =  ("", "/org/a11y/atspi/null");
    public (string, ObjectPath) Application =  ("", "/org/a11y/atspi/null");
    public (string, ObjectPath) Parent =  ("", "/org/a11y/atspi/null");
    public int IndexInParent = 0;
    public int ChildCount = 0;
    public string[] ApplicableInterfaces = ["org.a11y.atspi.Accessible"];
    public string Name = string.Empty;
    public AtSpiConstants.Role Role = default;
    public string Description = string.Empty;
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
        Name,
        (uint)Role,
        Description,
        ApplicableStates);
}
