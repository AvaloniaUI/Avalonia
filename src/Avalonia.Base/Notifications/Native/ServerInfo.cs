using System;

namespace Avalonia.Notifications.Native
{
    public struct ServerInfo
    {
        public ServerInfo(string name, string vendor, string version, string specificationsVersion)
        {
            Name = name;
            Vendor = vendor;
            Version = version;
            SpecificationsVersion = specificationsVersion;
        }

        public string Name { get; }

        public string Vendor { get; }

        public string Version { get; }

        public string SpecificationsVersion { get; }

        public override string ToString()
        {
            return $"Name: {Name}{Environment.NewLine}"
                   + $"Vendor: {Vendor}{Environment.NewLine}"
                   + $"Version: {Version}{Environment.NewLine}"
                   + $"Specifications Version: {SpecificationsVersion}{Environment.NewLine}";
        }
    }
}
