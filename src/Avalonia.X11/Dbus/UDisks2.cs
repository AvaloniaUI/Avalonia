using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Tmds.DBus;

[assembly: InternalsVisibleTo(Tmds.DBus.Connection.DynamicAssemblyName)]
namespace Avalonia.X11.Dbus
{
    [DBusInterface("org.freedesktop.DBus.ObjectManager")]
    interface IObjectManager : IDBusObject
    {
        Task<IDictionary<ObjectPath, IDictionary<string, IDictionary<string, object>>>> GetManagedObjectsAsync();
        Task<IDisposable> WatchInterfacesAddedAsync(Action<(ObjectPath objectPath, IDictionary<string, IDictionary<string, object>> interfacesAndProperties)> handler, Action<Exception> onError = null);
        Task<IDisposable> WatchInterfacesRemovedAsync(Action<(ObjectPath objectPath, string[] interfaces)> handler, Action<Exception> onError = null);
    }

    [DBusInterface("org.freedesktop.UDisks2.Manager")]
    interface IManager : IDBusObject
    {
        Task<(bool available, string)> CanFormatAsync(string Type);
        Task<(bool available, ulong, string)> CanResizeAsync(string Type);
        Task<(bool available, string)> CanCheckAsync(string Type);
        Task<(bool available, string)> CanRepairAsync(string Type);
        Task<ObjectPath> LoopSetupAsync(CloseSafeHandle Fd, IDictionary<string, object> Options);
        Task<ObjectPath> MDRaidCreateAsync(ObjectPath[] Blocks, string Level, string Name, ulong Chunk, IDictionary<string, object> Options);
        Task EnableModulesAsync(bool Enable);
        Task<ObjectPath[]> GetBlockDevicesAsync(IDictionary<string, object> Options);
        Task<ObjectPath[]> ResolveDeviceAsync(IDictionary<string, object> Devspec, IDictionary<string, object> Options);
        Task<T> GetAsync<T>(string prop);
        Task<ManagerProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class ManagerProperties
    {
        private string _Version = default(string);
        public string Version
        {
            get
            {
                return _Version;
            }

            set
            {
                _Version = (value);
            }
        }

        private string[] _SupportedFilesystems = default(string[]);
        public string[] SupportedFilesystems
        {
            get
            {
                return _SupportedFilesystems;
            }

            set
            {
                _SupportedFilesystems = (value);
            }
        }
    }

    static class ManagerExtensions
    {
        public static Task<string> GetVersionAsync(this IManager o) => o.GetAsync<string>("Version");
        public static Task<string[]> GetSupportedFilesystemsAsync(this IManager o) => o.GetAsync<string[]>("SupportedFilesystems");
    }

    [DBusInterface("org.freedesktop.UDisks2.Drive.Ata")]
    interface IAta : IDBusObject
    {
        Task SmartUpdateAsync(IDictionary<string, object> Options);
        Task<(byte, string, ushort, int, int, int, long, int, IDictionary<string, object>)[]> SmartGetAttributesAsync(IDictionary<string, object> Options);
        Task SmartSelftestStartAsync(string Type, IDictionary<string, object> Options);
        Task SmartSelftestAbortAsync(IDictionary<string, object> Options);
        Task SmartSetEnabledAsync(bool Value, IDictionary<string, object> Options);
        Task<byte> PmGetStateAsync(IDictionary<string, object> Options);
        Task PmStandbyAsync(IDictionary<string, object> Options);
        Task PmWakeupAsync(IDictionary<string, object> Options);
        Task SecurityEraseUnitAsync(IDictionary<string, object> Options);
        Task<T> GetAsync<T>(string prop);
        Task<AtaProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class AtaProperties
    {
        private bool _SmartSupported = default(bool);
        public bool SmartSupported
        {
            get
            {
                return _SmartSupported;
            }

            set
            {
                _SmartSupported = (value);
            }
        }

        private bool _SmartEnabled = default(bool);
        public bool SmartEnabled
        {
            get
            {
                return _SmartEnabled;
            }

            set
            {
                _SmartEnabled = (value);
            }
        }

        private ulong _SmartUpdated = default(ulong);
        public ulong SmartUpdated
        {
            get
            {
                return _SmartUpdated;
            }

            set
            {
                _SmartUpdated = (value);
            }
        }

        private bool _SmartFailing = default(bool);
        public bool SmartFailing
        {
            get
            {
                return _SmartFailing;
            }

            set
            {
                _SmartFailing = (value);
            }
        }

        private ulong _SmartPowerOnSeconds = default(ulong);
        public ulong SmartPowerOnSeconds
        {
            get
            {
                return _SmartPowerOnSeconds;
            }

            set
            {
                _SmartPowerOnSeconds = (value);
            }
        }

        private double _SmartTemperature = default(double);
        public double SmartTemperature
        {
            get
            {
                return _SmartTemperature;
            }

            set
            {
                _SmartTemperature = (value);
            }
        }

        private int _SmartNumAttributesFailing = default(int);
        public int SmartNumAttributesFailing
        {
            get
            {
                return _SmartNumAttributesFailing;
            }

            set
            {
                _SmartNumAttributesFailing = (value);
            }
        }

        private int _SmartNumAttributesFailedInThePast = default(int);
        public int SmartNumAttributesFailedInThePast
        {
            get
            {
                return _SmartNumAttributesFailedInThePast;
            }

            set
            {
                _SmartNumAttributesFailedInThePast = (value);
            }
        }

        private long _SmartNumBadSectors = default(long);
        public long SmartNumBadSectors
        {
            get
            {
                return _SmartNumBadSectors;
            }

            set
            {
                _SmartNumBadSectors = (value);
            }
        }

        private string _SmartSelftestStatus = default(string);
        public string SmartSelftestStatus
        {
            get
            {
                return _SmartSelftestStatus;
            }

            set
            {
                _SmartSelftestStatus = (value);
            }
        }

        private int _SmartSelftestPercentRemaining = default(int);
        public int SmartSelftestPercentRemaining
        {
            get
            {
                return _SmartSelftestPercentRemaining;
            }

            set
            {
                _SmartSelftestPercentRemaining = (value);
            }
        }

        private bool _PmSupported = default(bool);
        public bool PmSupported
        {
            get
            {
                return _PmSupported;
            }

            set
            {
                _PmSupported = (value);
            }
        }

        private bool _PmEnabled = default(bool);
        public bool PmEnabled
        {
            get
            {
                return _PmEnabled;
            }

            set
            {
                _PmEnabled = (value);
            }
        }

        private bool _ApmSupported = default(bool);
        public bool ApmSupported
        {
            get
            {
                return _ApmSupported;
            }

            set
            {
                _ApmSupported = (value);
            }
        }

        private bool _ApmEnabled = default(bool);
        public bool ApmEnabled
        {
            get
            {
                return _ApmEnabled;
            }

            set
            {
                _ApmEnabled = (value);
            }
        }

        private bool _AamSupported = default(bool);
        public bool AamSupported
        {
            get
            {
                return _AamSupported;
            }

            set
            {
                _AamSupported = (value);
            }
        }

        private bool _AamEnabled = default(bool);
        public bool AamEnabled
        {
            get
            {
                return _AamEnabled;
            }

            set
            {
                _AamEnabled = (value);
            }
        }

        private int _AamVendorRecommendedValue = default(int);
        public int AamVendorRecommendedValue
        {
            get
            {
                return _AamVendorRecommendedValue;
            }

            set
            {
                _AamVendorRecommendedValue = (value);
            }
        }

        private bool _WriteCacheSupported = default(bool);
        public bool WriteCacheSupported
        {
            get
            {
                return _WriteCacheSupported;
            }

            set
            {
                _WriteCacheSupported = (value);
            }
        }

        private bool _WriteCacheEnabled = default(bool);
        public bool WriteCacheEnabled
        {
            get
            {
                return _WriteCacheEnabled;
            }

            set
            {
                _WriteCacheEnabled = (value);
            }
        }

        private bool _ReadLookaheadSupported = default(bool);
        public bool ReadLookaheadSupported
        {
            get
            {
                return _ReadLookaheadSupported;
            }

            set
            {
                _ReadLookaheadSupported = (value);
            }
        }

        private bool _ReadLookaheadEnabled = default(bool);
        public bool ReadLookaheadEnabled
        {
            get
            {
                return _ReadLookaheadEnabled;
            }

            set
            {
                _ReadLookaheadEnabled = (value);
            }
        }

        private int _SecurityEraseUnitMinutes = default(int);
        public int SecurityEraseUnitMinutes
        {
            get
            {
                return _SecurityEraseUnitMinutes;
            }

            set
            {
                _SecurityEraseUnitMinutes = (value);
            }
        }

        private int _SecurityEnhancedEraseUnitMinutes = default(int);
        public int SecurityEnhancedEraseUnitMinutes
        {
            get
            {
                return _SecurityEnhancedEraseUnitMinutes;
            }

            set
            {
                _SecurityEnhancedEraseUnitMinutes = (value);
            }
        }

        private bool _SecurityFrozen = default(bool);
        public bool SecurityFrozen
        {
            get
            {
                return _SecurityFrozen;
            }

            set
            {
                _SecurityFrozen = (value);
            }
        }
    }

    static class AtaExtensions
    {
        public static Task<bool> GetSmartSupportedAsync(this IAta o) => o.GetAsync<bool>("SmartSupported");
        public static Task<bool> GetSmartEnabledAsync(this IAta o) => o.GetAsync<bool>("SmartEnabled");
        public static Task<ulong> GetSmartUpdatedAsync(this IAta o) => o.GetAsync<ulong>("SmartUpdated");
        public static Task<bool> GetSmartFailingAsync(this IAta o) => o.GetAsync<bool>("SmartFailing");
        public static Task<ulong> GetSmartPowerOnSecondsAsync(this IAta o) => o.GetAsync<ulong>("SmartPowerOnSeconds");
        public static Task<double> GetSmartTemperatureAsync(this IAta o) => o.GetAsync<double>("SmartTemperature");
        public static Task<int> GetSmartNumAttributesFailingAsync(this IAta o) => o.GetAsync<int>("SmartNumAttributesFailing");
        public static Task<int> GetSmartNumAttributesFailedInThePastAsync(this IAta o) => o.GetAsync<int>("SmartNumAttributesFailedInThePast");
        public static Task<long> GetSmartNumBadSectorsAsync(this IAta o) => o.GetAsync<long>("SmartNumBadSectors");
        public static Task<string> GetSmartSelftestStatusAsync(this IAta o) => o.GetAsync<string>("SmartSelftestStatus");
        public static Task<int> GetSmartSelftestPercentRemainingAsync(this IAta o) => o.GetAsync<int>("SmartSelftestPercentRemaining");
        public static Task<bool> GetPmSupportedAsync(this IAta o) => o.GetAsync<bool>("PmSupported");
        public static Task<bool> GetPmEnabledAsync(this IAta o) => o.GetAsync<bool>("PmEnabled");
        public static Task<bool> GetApmSupportedAsync(this IAta o) => o.GetAsync<bool>("ApmSupported");
        public static Task<bool> GetApmEnabledAsync(this IAta o) => o.GetAsync<bool>("ApmEnabled");
        public static Task<bool> GetAamSupportedAsync(this IAta o) => o.GetAsync<bool>("AamSupported");
        public static Task<bool> GetAamEnabledAsync(this IAta o) => o.GetAsync<bool>("AamEnabled");
        public static Task<int> GetAamVendorRecommendedValueAsync(this IAta o) => o.GetAsync<int>("AamVendorRecommendedValue");
        public static Task<bool> GetWriteCacheSupportedAsync(this IAta o) => o.GetAsync<bool>("WriteCacheSupported");
        public static Task<bool> GetWriteCacheEnabledAsync(this IAta o) => o.GetAsync<bool>("WriteCacheEnabled");
        public static Task<bool> GetReadLookaheadSupportedAsync(this IAta o) => o.GetAsync<bool>("ReadLookaheadSupported");
        public static Task<bool> GetReadLookaheadEnabledAsync(this IAta o) => o.GetAsync<bool>("ReadLookaheadEnabled");
        public static Task<int> GetSecurityEraseUnitMinutesAsync(this IAta o) => o.GetAsync<int>("SecurityEraseUnitMinutes");
        public static Task<int> GetSecurityEnhancedEraseUnitMinutesAsync(this IAta o) => o.GetAsync<int>("SecurityEnhancedEraseUnitMinutes");
        public static Task<bool> GetSecurityFrozenAsync(this IAta o) => o.GetAsync<bool>("SecurityFrozen");
    }

    [DBusInterface("org.freedesktop.UDisks2.Drive")]
    interface IDrive : IDBusObject
    {
        Task EjectAsync(IDictionary<string, object> Options);
        Task SetConfigurationAsync(IDictionary<string, object> Value, IDictionary<string, object> Options);
        Task PowerOffAsync(IDictionary<string, object> Options);
        Task<T> GetAsync<T>(string prop);
        Task<DriveProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class DriveProperties
    {
        private string _Vendor = default(string);
        public string Vendor
        {
            get
            {
                return _Vendor;
            }

            set
            {
                _Vendor = (value);
            }
        }

        private string _Model = default(string);
        public string Model
        {
            get
            {
                return _Model;
            }

            set
            {
                _Model = (value);
            }
        }

        private string _Revision = default(string);
        public string Revision
        {
            get
            {
                return _Revision;
            }

            set
            {
                _Revision = (value);
            }
        }

        private string _Serial = default(string);
        public string Serial
        {
            get
            {
                return _Serial;
            }

            set
            {
                _Serial = (value);
            }
        }

        private string _WWN = default(string);
        public string WWN
        {
            get
            {
                return _WWN;
            }

            set
            {
                _WWN = (value);
            }
        }

        private string _Id = default(string);
        public string Id
        {
            get
            {
                return _Id;
            }

            set
            {
                _Id = (value);
            }
        }

        private IDictionary<string, object> _Configuration = default(IDictionary<string, object>);
        public IDictionary<string, object> Configuration
        {
            get
            {
                return _Configuration;
            }

            set
            {
                _Configuration = (value);
            }
        }

        private string _Media = default(string);
        public string Media
        {
            get
            {
                return _Media;
            }

            set
            {
                _Media = (value);
            }
        }

        private string[] _MediaCompatibility = default(string[]);
        public string[] MediaCompatibility
        {
            get
            {
                return _MediaCompatibility;
            }

            set
            {
                _MediaCompatibility = (value);
            }
        }

        private bool _MediaRemovable = default(bool);
        public bool MediaRemovable
        {
            get
            {
                return _MediaRemovable;
            }

            set
            {
                _MediaRemovable = (value);
            }
        }

        private bool _MediaAvailable = default(bool);
        public bool MediaAvailable
        {
            get
            {
                return _MediaAvailable;
            }

            set
            {
                _MediaAvailable = (value);
            }
        }

        private bool _MediaChangeDetected = default(bool);
        public bool MediaChangeDetected
        {
            get
            {
                return _MediaChangeDetected;
            }

            set
            {
                _MediaChangeDetected = (value);
            }
        }

        private ulong _Size = default(ulong);
        public ulong Size
        {
            get
            {
                return _Size;
            }

            set
            {
                _Size = (value);
            }
        }

        private ulong _TimeDetected = default(ulong);
        public ulong TimeDetected
        {
            get
            {
                return _TimeDetected;
            }

            set
            {
                _TimeDetected = (value);
            }
        }

        private ulong _TimeMediaDetected = default(ulong);
        public ulong TimeMediaDetected
        {
            get
            {
                return _TimeMediaDetected;
            }

            set
            {
                _TimeMediaDetected = (value);
            }
        }

        private bool _Optical = default(bool);
        public bool Optical
        {
            get
            {
                return _Optical;
            }

            set
            {
                _Optical = (value);
            }
        }

        private bool _OpticalBlank = default(bool);
        public bool OpticalBlank
        {
            get
            {
                return _OpticalBlank;
            }

            set
            {
                _OpticalBlank = (value);
            }
        }

        private uint _OpticalNumTracks = default(uint);
        public uint OpticalNumTracks
        {
            get
            {
                return _OpticalNumTracks;
            }

            set
            {
                _OpticalNumTracks = (value);
            }
        }

        private uint _OpticalNumAudioTracks = default(uint);
        public uint OpticalNumAudioTracks
        {
            get
            {
                return _OpticalNumAudioTracks;
            }

            set
            {
                _OpticalNumAudioTracks = (value);
            }
        }

        private uint _OpticalNumDataTracks = default(uint);
        public uint OpticalNumDataTracks
        {
            get
            {
                return _OpticalNumDataTracks;
            }

            set
            {
                _OpticalNumDataTracks = (value);
            }
        }

        private uint _OpticalNumSessions = default(uint);
        public uint OpticalNumSessions
        {
            get
            {
                return _OpticalNumSessions;
            }

            set
            {
                _OpticalNumSessions = (value);
            }
        }

        private int _RotationRate = default(int);
        public int RotationRate
        {
            get
            {
                return _RotationRate;
            }

            set
            {
                _RotationRate = (value);
            }
        }

        private string _ConnectionBus = default(string);
        public string ConnectionBus
        {
            get
            {
                return _ConnectionBus;
            }

            set
            {
                _ConnectionBus = (value);
            }
        }

        private string _Seat = default(string);
        public string Seat
        {
            get
            {
                return _Seat;
            }

            set
            {
                _Seat = (value);
            }
        }

        private bool _Removable = default(bool);
        public bool Removable
        {
            get
            {
                return _Removable;
            }

            set
            {
                _Removable = (value);
            }
        }

        private bool _Ejectable = default(bool);
        public bool Ejectable
        {
            get
            {
                return _Ejectable;
            }

            set
            {
                _Ejectable = (value);
            }
        }

        private string _SortKey = default(string);
        public string SortKey
        {
            get
            {
                return _SortKey;
            }

            set
            {
                _SortKey = (value);
            }
        }

        private bool _CanPowerOff = default(bool);
        public bool CanPowerOff
        {
            get
            {
                return _CanPowerOff;
            }

            set
            {
                _CanPowerOff = (value);
            }
        }

        private string _SiblingId = default(string);
        public string SiblingId
        {
            get
            {
                return _SiblingId;
            }

            set
            {
                _SiblingId = (value);
            }
        }
    }

    static class DriveExtensions
    {
        public static Task<string> GetVendorAsync(this IDrive o) => o.GetAsync<string>("Vendor");
        public static Task<string> GetModelAsync(this IDrive o) => o.GetAsync<string>("Model");
        public static Task<string> GetRevisionAsync(this IDrive o) => o.GetAsync<string>("Revision");
        public static Task<string> GetSerialAsync(this IDrive o) => o.GetAsync<string>("Serial");
        public static Task<string> GetWWNAsync(this IDrive o) => o.GetAsync<string>("WWN");
        public static Task<string> GetIdAsync(this IDrive o) => o.GetAsync<string>("Id");
        public static Task<IDictionary<string, object>> GetConfigurationAsync(this IDrive o) => o.GetAsync<IDictionary<string, object>>("Configuration");
        public static Task<string> GetMediaAsync(this IDrive o) => o.GetAsync<string>("Media");
        public static Task<string[]> GetMediaCompatibilityAsync(this IDrive o) => o.GetAsync<string[]>("MediaCompatibility");
        public static Task<bool> GetMediaRemovableAsync(this IDrive o) => o.GetAsync<bool>("MediaRemovable");
        public static Task<bool> GetMediaAvailableAsync(this IDrive o) => o.GetAsync<bool>("MediaAvailable");
        public static Task<bool> GetMediaChangeDetectedAsync(this IDrive o) => o.GetAsync<bool>("MediaChangeDetected");
        public static Task<ulong> GetSizeAsync(this IDrive o) => o.GetAsync<ulong>("Size");
        public static Task<ulong> GetTimeDetectedAsync(this IDrive o) => o.GetAsync<ulong>("TimeDetected");
        public static Task<ulong> GetTimeMediaDetectedAsync(this IDrive o) => o.GetAsync<ulong>("TimeMediaDetected");
        public static Task<bool> GetOpticalAsync(this IDrive o) => o.GetAsync<bool>("Optical");
        public static Task<bool> GetOpticalBlankAsync(this IDrive o) => o.GetAsync<bool>("OpticalBlank");
        public static Task<uint> GetOpticalNumTracksAsync(this IDrive o) => o.GetAsync<uint>("OpticalNumTracks");
        public static Task<uint> GetOpticalNumAudioTracksAsync(this IDrive o) => o.GetAsync<uint>("OpticalNumAudioTracks");
        public static Task<uint> GetOpticalNumDataTracksAsync(this IDrive o) => o.GetAsync<uint>("OpticalNumDataTracks");
        public static Task<uint> GetOpticalNumSessionsAsync(this IDrive o) => o.GetAsync<uint>("OpticalNumSessions");
        public static Task<int> GetRotationRateAsync(this IDrive o) => o.GetAsync<int>("RotationRate");
        public static Task<string> GetConnectionBusAsync(this IDrive o) => o.GetAsync<string>("ConnectionBus");
        public static Task<string> GetSeatAsync(this IDrive o) => o.GetAsync<string>("Seat");
        public static Task<bool> GetRemovableAsync(this IDrive o) => o.GetAsync<bool>("Removable");
        public static Task<bool> GetEjectableAsync(this IDrive o) => o.GetAsync<bool>("Ejectable");
        public static Task<string> GetSortKeyAsync(this IDrive o) => o.GetAsync<string>("SortKey");
        public static Task<bool> GetCanPowerOffAsync(this IDrive o) => o.GetAsync<bool>("CanPowerOff");
        public static Task<string> GetSiblingIdAsync(this IDrive o) => o.GetAsync<string>("SiblingId");
    }

    [DBusInterface("org.freedesktop.UDisks2.Loop")]
    interface ILoop : IDBusObject
    {
        Task DeleteAsync(IDictionary<string, object> Options);
        Task SetAutoclearAsync(bool Value, IDictionary<string, object> Options);
        Task<T> GetAsync<T>(string prop);
        Task<LoopProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class LoopProperties
    {
        private byte[] _BackingFile = default(byte[]);
        public byte[] BackingFile
        {
            get
            {
                return _BackingFile;
            }

            set
            {
                _BackingFile = (value);
            }
        }

        private bool _Autoclear = default(bool);
        public bool Autoclear
        {
            get
            {
                return _Autoclear;
            }

            set
            {
                _Autoclear = (value);
            }
        }

        private uint _SetupByUID = default(uint);
        public uint SetupByUID
        {
            get
            {
                return _SetupByUID;
            }

            set
            {
                _SetupByUID = (value);
            }
        }
    }

    static class LoopExtensions
    {
        public static Task<byte[]> GetBackingFileAsync(this ILoop o) => o.GetAsync<byte[]>("BackingFile");
        public static Task<bool> GetAutoclearAsync(this ILoop o) => o.GetAsync<bool>("Autoclear");
        public static Task<uint> GetSetupByUIDAsync(this ILoop o) => o.GetAsync<uint>("SetupByUID");
    }

    [DBusInterface("org.freedesktop.UDisks2.Block")]
    interface IBlock : IDBusObject
    {
        Task AddConfigurationItemAsync((string, IDictionary<string, object>) Item, IDictionary<string, object> Options);
        Task RemoveConfigurationItemAsync((string, IDictionary<string, object>) Item, IDictionary<string, object> Options);
        Task UpdateConfigurationItemAsync((string, IDictionary<string, object>) OldItem, (string, IDictionary<string, object>) NewItem, IDictionary<string, object> Options);
        Task<(string, IDictionary<string, object>)[]> GetSecretConfigurationAsync(IDictionary<string, object> Options);
        Task FormatAsync(string Type, IDictionary<string, object> Options);
        Task<CloseSafeHandle> OpenForBackupAsync(IDictionary<string, object> Options);
        Task<CloseSafeHandle> OpenForRestoreAsync(IDictionary<string, object> Options);
        Task<CloseSafeHandle> OpenForBenchmarkAsync(IDictionary<string, object> Options);
        Task<CloseSafeHandle> OpenDeviceAsync(string Mode, IDictionary<string, object> Options);
        Task RescanAsync(IDictionary<string, object> Options);
        Task<T> GetAsync<T>(string prop);
        Task<BlockProperties> GetAllAsync();
        Task SetAsync(string prop, object val);
        Task<IDisposable> WatchPropertiesAsync(Action<PropertyChanges> handler);
    }

    [Dictionary]
    class BlockProperties
    {
        private byte[] _Device = default(byte[]);
        public byte[] Device
        {
            get
            {
                return _Device;
            }

            set
            {
                _Device = (value);
            }
        }

        private byte[] _PreferredDevice = default(byte[]);
        public byte[] PreferredDevice
        {
            get
            {
                return _PreferredDevice;
            }

            set
            {
                _PreferredDevice = (value);
            }
        }

        private byte[][] _Symlinks = default(byte[][]);
        public byte[][] Symlinks
        {
            get
            {
                return _Symlinks;
            }

            set
            {
                _Symlinks = (value);
            }
        }

        private ulong _DeviceNumber = default(ulong);
        public ulong DeviceNumber
        {
            get
            {
                return _DeviceNumber;
            }

            set
            {
                _DeviceNumber = (value);
            }
        }

        private string _Id = default(string);
        public string Id
        {
            get
            {
                return _Id;
            }

            set
            {
                _Id = (value);
            }
        }

        private ulong _Size = default(ulong);
        public ulong Size
        {
            get
            {
                return _Size;
            }

            set
            {
                _Size = (value);
            }
        }

        private bool _ReadOnly = default(bool);
        public bool ReadOnly
        {
            get
            {
                return _ReadOnly;
            }

            set
            {
                _ReadOnly = (value);
            }
        }

        private ObjectPath _Drive = default(ObjectPath);
        public ObjectPath Drive
        {
            get
            {
                return _Drive;
            }

            set
            {
                _Drive = (value);
            }
        }

        private ObjectPath _MDRaid = default(ObjectPath);
        public ObjectPath MDRaid
        {
            get
            {
                return _MDRaid;
            }

            set
            {
                _MDRaid = (value);
            }
        }

        private ObjectPath _MDRaidMember = default(ObjectPath);
        public ObjectPath MDRaidMember
        {
            get
            {
                return _MDRaidMember;
            }

            set
            {
                _MDRaidMember = (value);
            }
        }

        private string _IdUsage = default(string);
        public string IdUsage
        {
            get
            {
                return _IdUsage;
            }

            set
            {
                _IdUsage = (value);
            }
        }

        private string _IdType = default(string);
        public string IdType
        {
            get
            {
                return _IdType;
            }

            set
            {
                _IdType = (value);
            }
        }

        private string _IdVersion = default(string);
        public string IdVersion
        {
            get
            {
                return _IdVersion;
            }

            set
            {
                _IdVersion = (value);
            }
        }

        private string _IdLabel = default(string);
        public string IdLabel
        {
            get
            {
                return _IdLabel;
            }

            set
            {
                _IdLabel = (value);
            }
        }

        private string _IdUUID = default(string);
        public string IdUUID
        {
            get
            {
                return _IdUUID;
            }

            set
            {
                _IdUUID = (value);
            }
        }

        private (string, IDictionary<string, object>)[] _Configuration = default((string, IDictionary<string, object>)[]);
        public (string, IDictionary<string, object>)[] Configuration
        {
            get
            {
                return _Configuration;
            }

            set
            {
                _Configuration = (value);
            }
        }

        private ObjectPath _CryptoBackingDevice = default(ObjectPath);
        public ObjectPath CryptoBackingDevice
        {
            get
            {
                return _CryptoBackingDevice;
            }

            set
            {
                _CryptoBackingDevice = (value);
            }
        }

        private bool _HintPartitionable = default(bool);
        public bool HintPartitionable
        {
            get
            {
                return _HintPartitionable;
            }

            set
            {
                _HintPartitionable = (value);
            }
        }

        private bool _HintSystem = default(bool);
        public bool HintSystem
        {
            get
            {
                return _HintSystem;
            }

            set
            {
                _HintSystem = (value);
            }
        }

        private bool _HintIgnore = default(bool);
        public bool HintIgnore
        {
            get
            {
                return _HintIgnore;
            }

            set
            {
                _HintIgnore = (value);
            }
        }

        private bool _HintAuto = default(bool);
        public bool HintAuto
        {
            get
            {
                return _HintAuto;
            }

            set
            {
                _HintAuto = (value);
            }
        }

        private string _HintName = default(string);
        public string HintName
        {
            get
            {
                return _HintName;
            }

            set
            {
                _HintName = (value);
            }
        }

        private string _HintIconName = default(string);
        public string HintIconName
        {
            get
            {
                return _HintIconName;
            }

            set
            {
                _HintIconName = (value);
            }
        }

        private string _HintSymbolicIconName = default(string);
        public string HintSymbolicIconName
        {
            get
            {
                return _HintSymbolicIconName;
            }

            set
            {
                _HintSymbolicIconName = (value);
            }
        }

        private string[] _UserspaceMountOptions = default(string[]);
        public string[] UserspaceMountOptions
        {
            get
            {
                return _UserspaceMountOptions;
            }

            set
            {
                _UserspaceMountOptions = (value);
            }
        }
    }

    static class BlockExtensions
    {
        public static Task<byte[]> GetDeviceAsync(this IBlock o) => o.GetAsync<byte[]>("Device");
        public static Task<byte[]> GetPreferredDeviceAsync(this IBlock o) => o.GetAsync<byte[]>("PreferredDevice");
        public static Task<byte[][]> GetSymlinksAsync(this IBlock o) => o.GetAsync<byte[][]>("Symlinks");
        public static Task<ulong> GetDeviceNumberAsync(this IBlock o) => o.GetAsync<ulong>("DeviceNumber");
        public static Task<string> GetIdAsync(this IBlock o) => o.GetAsync<string>("Id");
        public static Task<ulong> GetSizeAsync(this IBlock o) => o.GetAsync<ulong>("Size");
        public static Task<bool> GetReadOnlyAsync(this IBlock o) => o.GetAsync<bool>("ReadOnly");
        public static Task<ObjectPath> GetDriveAsync(this IBlock o) => o.GetAsync<ObjectPath>("Drive");
        public static Task<ObjectPath> GetMDRaidAsync(this IBlock o) => o.GetAsync<ObjectPath>("MDRaid");
        public static Task<ObjectPath> GetMDRaidMemberAsync(this IBlock o) => o.GetAsync<ObjectPath>("MDRaidMember");
        public static Task<string> GetIdUsageAsync(this IBlock o) => o.GetAsync<string>("IdUsage");
        public static Task<string> GetIdTypeAsync(this IBlock o) => o.GetAsync<string>("IdType");
        public static Task<string> GetIdVersionAsync(this IBlock o) => o.GetAsync<string>("IdVersion");
        public static Task<string> GetIdLabelAsync(this IBlock o) => o.GetAsync<string>("IdLabel");
        public static Task<string> GetIdUUIDAsync(this IBlock o) => o.GetAsync<string>("IdUUID");
        public static Task<(string, IDictionary<string, object>)[]> GetConfigurationAsync(this IBlock o) => o.GetAsync<(string, IDictionary<string, object>)[]>("Configuration");
        public static Task<ObjectPath> GetCryptoBackingDeviceAsync(this IBlock o) => o.GetAsync<ObjectPath>("CryptoBackingDevice");
        public static Task<bool> GetHintPartitionableAsync(this IBlock o) => o.GetAsync<bool>("HintPartitionable");
        public static Task<bool> GetHintSystemAsync(this IBlock o) => o.GetAsync<bool>("HintSystem");
        public static Task<bool> GetHintIgnoreAsync(this IBlock o) => o.GetAsync<bool>("HintIgnore");
        public static Task<bool> GetHintAutoAsync(this IBlock o) => o.GetAsync<bool>("HintAuto");
        public static Task<string> GetHintNameAsync(this IBlock o) => o.GetAsync<string>("HintName");
        public static Task<string> GetHintIconNameAsync(this IBlock o) => o.GetAsync<string>("HintIconName");
        public static Task<string> GetHintSymbolicIconNameAsync(this IBlock o) => o.GetAsync<string>("HintSymbolicIconName");
        public static Task<string[]> GetUserspaceMountOptionsAsync(this IBlock o) => o.GetAsync<string[]>("UserspaceMountOptions");
    }
}
