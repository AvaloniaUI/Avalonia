namespace Avalonia.Remote.Protocol.Designer
{
    [AvaloniaRemoteMessageGuid("9AEC9A2E-6315-4066-B4BA-E9A9EFD0F8CC")]
    public class UpdateXamlMessage
    {
        public string Xaml { get; set; }
        public string AssemblyPath { get; set; }
    }

    [AvaloniaRemoteMessageGuid("B7A70093-0C5D-47FD-9261-22086D43A2E2")]
    public class UpdateXamlResultMessage
    {
        public string Error { get; set; }
    }
    
    
}