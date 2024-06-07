namespace Tmds.DBus.Protocol;

public class ClientSetupResult
{
    public ClientSetupResult(string address)
    {
        ConnectionAddress = address ?? throw new ArgumentNullException(nameof(address));
    }

    public string ConnectionAddress { get;  }

    public object? TeardownToken { get; set; }

    public string? UserId { get; set; }

    public string? MachineId { get; set; }

    public bool SupportsFdPassing { get; set; }
}