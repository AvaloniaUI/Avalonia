#include "common.h"

class PlatformRenderTimer : public ComSingleObject<IAvnPlatformRenderTimer, &IID_IAvnPlatformRenderTimer>
{
private:
    ComPtr<IAvnActionCallback> _callback;
    CVDisplayLinkRef _displayLink = nil;

public:
    FORWARD_IUNKNOWN()

    virtual ~PlatformRenderTimer()
    {
        if (_displayLink != nil)
        {
            // Stops the display link thread, so OnTick never sees a destroyed object
            CVDisplayLinkStop(_displayLink);
            CVDisplayLinkRelease(_displayLink);
        }
    }
    virtual int RegisterTick (
        const ComPtr<IAvnActionCallback>& callback) override
    {
        @autoreleasepool
        {
            if (_displayLink != nil)
            {
                return E_UNEXPECTED;
            }

            _callback = callback;
            auto result = CVDisplayLinkCreateWithActiveCGDisplays(&_displayLink);
            if (result != 0)
            {
                return result;
            }

            result = CVDisplayLinkSetOutputCallback(_displayLink, OnTick, this);
            if (result != 0)
            {
                return result;
            }
        }
        return S_OK;
    }

    virtual void Start () override
    {
        @autoreleasepool
        {
            if (CVDisplayLinkIsRunning(_displayLink) == false) {
                CVDisplayLinkStart(_displayLink);
            }
        }
    }

    virtual void Stop () override
    {
        @autoreleasepool
        {
            if (CVDisplayLinkIsRunning(_displayLink) == true) {
                CVDisplayLinkStop(_displayLink);
            }
        }
    }

    virtual bool RunsInBackground () override
    {
        @autoreleasepool
        {
            return true;
        }
    }
    
    static CVReturn OnTick(CVDisplayLinkRef displayLink, const CVTimeStamp *inNow, const CVTimeStamp *inOutputTime, CVOptionFlags flagsIn, CVOptionFlags *flagsOut, void *displayLinkContext)
    {
        START_ARP_CALL;
        PlatformRenderTimer *object = (PlatformRenderTimer *)displayLinkContext;
        object->_callback->Run();
        return kCVReturnSuccess;
    }
};

extern ComPtr<IAvnPlatformRenderTimer> CreatePlatformRenderTimer()
{
    return comnew<PlatformRenderTimer>();
}
