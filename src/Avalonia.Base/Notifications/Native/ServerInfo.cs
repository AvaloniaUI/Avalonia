using System;
using JetBrains.Annotations;

namespace Avalonia.Notifications.Native
{
    [PublicAPI]
    public struct ServerInfo
    {
        public ServerInfo(
            [CanBeNull] string name,
            [CanBeNull] string vendor,
            [CanBeNull] string version,
            [CanBeNull] string specificationsVersion
        )
        {
            Name = name;
            Vendor = vendor;
            Version = version;
            SpecificationsVersion = specificationsVersion;
        }

        /// <summary>
        /// Name of the notification server (eg. Gnome)
        /// </summary>
        [CanBeNull] public string Name { get; }

        /// <summary>
        /// The implementor of the notification server
        /// </summary>
        [CanBeNull] public string Vendor { get; }

        /// <summary>
        /// The version of the server (eg. Name: Gnome, Version: 3.32) 
        /// </summary>
        [CanBeNull] public string Version { get; }

        /// <summary>
        /// The specification version that the server implements
        /// </summary>
        [CanBeNull] public string SpecificationsVersion { get; }

        public override string ToString()
        {
            return $"Name: {Name}{Environment.NewLine}"
                   + $"Vendor: {Vendor}{Environment.NewLine}"
                   + $"Version: {Version}{Environment.NewLine}"
                   + $"Specifications Version: {SpecificationsVersion}{Environment.NewLine}";
        }
    }
}
