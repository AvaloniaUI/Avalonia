#include "common.h"

class PlatformRenderTimer : public ComSingleObject<IAvnPlatformRenderTimer, &IID_IAvnPlatformRenderTimer>
{
private:
    ComPtr<IAvnActionCallback> _callback;
    CVDisplayLinkRef _displayLink;
    bool _registered;
    bool _started;
    bool _waitingForDisplay;

public:
    FORWARD_IUNKNOWN()

    PlatformRenderTimer() : _displayLink(nil), _registered(false), _started(false), _waitingForDisplay(false) {}

    virtual ~PlatformRenderTimer()
    {
        CGDisplayRemoveReconfigurationCallback(DisplayReconfigurationCallback, this);

        if (_displayLink != nil)
        {
            CVDisplayLinkStop(_displayLink);
            CVDisplayLinkRelease(_displayLink);
            _displayLink = nil;
        }
    }

    bool TryCreateDisplayLink()
    {
        if (_displayLink != nil)
            return true;

        auto result = CVDisplayLinkCreateWithActiveCGDisplays(&_displayLink);
        if (result != 0)
        {
            _displayLink = nil;
            return false;
        }

        result = CVDisplayLinkSetOutputCallback(_displayLink, OnTick, this);
        if (result != 0)
        {
            CVDisplayLinkRelease(_displayLink);
            _displayLink = nil;
            return false;
        }

        return true;
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

            if (!TryCreateDisplayLink())
            {
                // No active display yet. Register for display reconfiguration
                // notifications so we can create the CVDisplayLink when a display
                // becomes available.
                _waitingForDisplay = true;
                CGDisplayRegisterReconfigurationCallback(DisplayReconfigurationCallback, this);
            }
        }
        return S_OK;
    }

    virtual void Start () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            _started = true;

            if (_displayLink != nil && CVDisplayLinkIsRunning(_displayLink) == false)
            {
                CVDisplayLinkStart(_displayLink);
            }
            // If no display link yet, _started flag ensures we start it
            // when a display becomes available via the reconfiguration callback.
        }
    }

    virtual void Stop () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            _started = false;

            if (_displayLink != nil && CVDisplayLinkIsRunning(_displayLink) == true)
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
            return true;
        }
    }

    void OnDisplayAdded()
    {
        if (!_waitingForDisplay)
            return;

        if (TryCreateDisplayLink())
        {
            _waitingForDisplay = false;
            CGDisplayRemoveReconfigurationCallback(DisplayReconfigurationCallback, this);

            if (_started)
            {
                CVDisplayLinkStart(_displayLink);
            }
        }
    }

    static void DisplayReconfigurationCallback(CGDirectDisplayID display,
        CGDisplayChangeSummaryFlags flags, void *userInfo)
    {
        if (flags & kCGDisplayAddFlag)
        {
            auto *timer = (PlatformRenderTimer *)userInfo;
            timer->OnDisplayAdded();
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
