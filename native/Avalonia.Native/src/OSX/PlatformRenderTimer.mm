#include "common.h"

class PlatformRenderTimer : public ComSingleObject<IAvnPlatformRenderTimer, &IID_IAvnPlatformRenderTimer>
{
private:
    ComPtr<IAvnActionCallback> _callback;
    CVDisplayLinkRef _displayLink;

public:
    FORWARD_IUNKNOWN()
    virtual HRESULT RegisterTick (
        IAvnActionCallback* callback) override
    {
        START_COM_CALL;
        
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
                return E_FAIL;
            }

            result = CVDisplayLinkSetOutputCallback(_displayLink, OnTick, this);
            if (result != 0)
            {
                return E_FAIL;
            }
        }
        return S_OK;
    }

    virtual void Start () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (CVDisplayLinkIsRunning(_displayLink) == false) {
                CVDisplayLinkStart(_displayLink);
            }
        }
    }

    virtual void Stop () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (CVDisplayLinkIsRunning(_displayLink) == true) {
                CVDisplayLinkStop(_displayLink);
            }
        }
    }

    virtual bool RunsInBackground () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            return true;
        }
    }
    
    static CVReturn OnTick(CVDisplayLinkRef displayLink, const CVTimeStamp *inNow, const CVTimeStamp *inOutputTime, CVOptionFlags flagsIn, CVOptionFlags *flagsOut, void *displayLinkContext)
    {
        PlatformRenderTimer *object = (PlatformRenderTimer *)displayLinkContext;
        object->_callback->Run();
        return kCVReturnSuccess;
    }
};

extern IAvnPlatformRenderTimer* CreatePlatformRenderTimer()
{
    return new PlatformRenderTimer();
}
