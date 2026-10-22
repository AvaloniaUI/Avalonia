#include "common.h"

namespace
{
    id<NSObject> s_inhibitAppSleepHandle{};
}

class PlatformBehaviorInhibition : public ComSingleObject<IAvnPlatformBehaviorInhibition, &IID_IAvnPlatformBehaviorInhibition>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual void SetInhibitAppSleep(bool inhibitAppSleep, char* reason) override
    {
        @autoreleasepool
        {
            if (inhibitAppSleep && s_inhibitAppSleepHandle == nullptr)
            {
                NSActivityOptions options = NSActivityUserInitiatedAllowingIdleSystemSleep;
                s_inhibitAppSleepHandle = [[NSProcessInfo processInfo] beginActivityWithOptions:options reason:[NSString stringWithUTF8String: reason]];
            }
            
            if (!inhibitAppSleep)
            {
                s_inhibitAppSleepHandle = nullptr;
            }
        }
    }
};

extern ComPtr<IAvnPlatformBehaviorInhibition> CreatePlatformBehaviorInhibition()
{
    return comnew<PlatformBehaviorInhibition>();
}
