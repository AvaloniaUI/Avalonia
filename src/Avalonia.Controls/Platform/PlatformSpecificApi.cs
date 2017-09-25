namespace Avalonia.Controls.Platform
{
    public class PlatformSpecificApi
    {
        private readonly BaseLinuxPlatformSpecificApiImpl _baseLinuxPlatformSpecificApi;
        private readonly BaseOSXPlatformSpecificApiImpl _baseOsxPlatformSpecificApi;
        private readonly BaseWin32PlatformSpecificApiImpl _baseWin32PlatformSpecificApi;

        public PlatformSpecificApi(IPlatformSpecificApiImpl impl)
        {
            _baseLinuxPlatformSpecificApi = impl as BaseLinuxPlatformSpecificApiImpl;
            _baseOsxPlatformSpecificApi = impl as BaseOSXPlatformSpecificApiImpl;
            _baseWin32PlatformSpecificApi = impl as BaseWin32PlatformSpecificApiImpl;
        }
    }
}