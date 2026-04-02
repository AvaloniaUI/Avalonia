#include "common.h"

class PlatformRenderTimer : public ComSingleObject<IAvnPlatformRenderTimer, &IID_IAvnPlatformRenderTimer>
{
private:
    ComPtr<IAvnActionCallback> _callback;
    CVDisplayLinkRef _displayLink;
    dispatch_source_t _fallbackTimer;
    bool _useFallbackTimer;
    bool _registered;

public:
    FORWARD_IUNKNOWN()

    PlatformRenderTimer() : _displayLink(nil), _fallbackTimer(nil), _useFallbackTimer(false), _registered(false) {}

    virtual ~PlatformRenderTimer()
    {
        if (_fallbackTimer != nil)
        {
            dispatch_source_cancel(_fallbackTimer);
            _fallbackTimer = nil;
        }
        if (_displayLink != nil)
        {
            CVDisplayLinkStop(_displayLink);
            CVDisplayLinkRelease(_displayLink);
            _displayLink = nil;
        }
    }

    virtual int RegisterTick (
        IAvnActionCallback* callback) override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (_registered)
            {
                return E_UNEXPECTED;
            }

            _registered = true;
            _callback = callback;
            auto result = CVDisplayLinkCreateWithActiveCGDisplays(&_displayLink);
            if (result != 0)
            {
                // CVDisplayLink unavailable (headless environment, no active display).
                // Fall back to a dispatch timer at ~60fps so the app can still start.
                _displayLink = nil;
                _useFallbackTimer = true;
                return S_OK;
            }

            result = CVDisplayLinkSetOutputCallback(_displayLink, OnTick, this);
            if (result != 0)
            {
                CVDisplayLinkRelease(_displayLink);
                _displayLink = nil;
                _useFallbackTimer = true;
                return S_OK;
            }
        }
        return S_OK;
    }

    virtual void Start () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (_useFallbackTimer)
            {
                if (_fallbackTimer == nil)
                {
                    _fallbackTimer = dispatch_source_create(
                        DISPATCH_SOURCE_TYPE_TIMER, 0, 0,
                        dispatch_get_main_queue());
                    auto callback = _callback;
                    dispatch_source_set_event_handler(_fallbackTimer, ^{
                        callback->Run();
                    });
                    // ~60fps: 16.67ms interval, 1ms leeway
                    dispatch_source_set_timer(_fallbackTimer,
                        dispatch_time(DISPATCH_TIME_NOW, 0),
                        NSEC_PER_SEC / 60,
                        NSEC_PER_MSEC);
                    dispatch_resume(_fallbackTimer);
                }
            }
            else if (_displayLink != nil && CVDisplayLinkIsRunning(_displayLink) == false)
            {
                CVDisplayLinkStart(_displayLink);
            }
        }
    }

    virtual void Stop () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            if (_useFallbackTimer)
            {
                if (_fallbackTimer != nil)
                {
                    dispatch_source_cancel(_fallbackTimer);
                    _fallbackTimer = nil;
                }
            }
            else if (_displayLink != nil && CVDisplayLinkIsRunning(_displayLink) == true)
            {
                CVDisplayLinkStop(_displayLink);
            }
        }
    }

    virtual bool RunsInBackground () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            // Fallback timer runs on main queue, not a background thread
            return !_useFallbackTimer;
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

extern IAvnPlatformRenderTimer* CreatePlatformRenderTimer()
{
    return new PlatformRenderTimer();
}
