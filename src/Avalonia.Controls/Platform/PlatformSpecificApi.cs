namespace Avalonia.Controls.Platform
{
    public class PlatformSpecificApi
    {
        private readonly BaseLinuxPlatformSpecificApiImpl _baseLinuxPlatformSpecificApi;
        private readonly BaseOSXPlatformSpecificApiImpl _baseOsxPlatformSpecificApi;
        private readonly BaseWindowsPlatformSpecificApiImpl _baseWindowsPlatformSpecificApi;

        public PlatformSpecificApi(IPlatformSpecificApiImpl impl)
        {
            _baseLinuxPlatformSpecificApi = impl as BaseLinuxPlatformSpecificApiImpl;
            _baseOsxPlatformSpecificApi = impl as BaseOSXPlatformSpecificApiImpl;
            _baseWindowsPlatformSpecificApi = impl as BaseWindowsPlatformSpecificApiImpl;
        }
    }
}