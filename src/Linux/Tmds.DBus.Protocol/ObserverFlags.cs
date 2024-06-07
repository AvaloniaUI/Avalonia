namespace Tmds.DBus.Protocol;

[Flags]
public enum ObserverFlags
{
    None = 0,
    EmitOnConnectionDispose = 1,
    EmitOnObserverDispose = 2,
    NoSubscribe = 4,

    EmitOnDispose = EmitOnConnectionDispose | EmitOnObserverDispose,
}
