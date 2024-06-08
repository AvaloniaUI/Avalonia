namespace Tmds.DBus.Protocol;

public class ClientConnectionOptions : ConnectionOptions
{
    private string _address;

    public ClientConnectionOptions(string address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));
        _address = address;
    }

    protected ClientConnectionOptions()
    {
        _address = string.Empty;
    }

    public bool AutoConnect { get; set; }

    internal bool IsShared { get; set; }

    protected internal virtual ValueTask<ClientSetupResult> SetupAsync(CancellationToken cancellationToken)
    {
        return new ValueTask<ClientSetupResult>(
            new ClientSetupResult(_address)
            {
                SupportsFdPassing = true,
                UserId = DBusEnvironment.UserId,
                MachineId = DBusEnvironment.MachineId
            });
    }

    protected internal virtual void Teardown(object? token)
    { }
}