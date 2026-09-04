#include "common.h"

class PlatformRenderTimer : public ComSingleObject<IAvnPlatformRenderTimer, &IID_IAvnPlatformRenderTimer>
{
private:
    ComPtr<IAvnActionCallback> _tick;
    ComPtr<IAvnActionCallback> _stateChanged;
    CVDisplayLinkRef _displayLink;
    bool _running;

    // Create the CVDisplayLink if we do not already have one. Returns true when a usable
    // link exists afterwards. CVDisplayLinkCreateWithActiveCGDisplays can fail with
    // kCVReturnInvalidArgument (-6661) while the display server is in a transient
    // reconfiguration state, e.g. shortly after wake-from-sleep when CGMainDisplayID() == 0
    // even though CGGetActiveDisplayList() reports active displays. In that case there is no
    // link to drive ticks and the managed timer takes over until a reconfiguration lets us
    // create one. See https://github.com/AvaloniaUI/Avalonia/issues/18895
    bool EnsureDisplayLink()
    {
        if (_displayLink != nil)
            return true;

        if (CVDisplayLinkCreateWithActiveCGDisplays(&_displayLink) != kCVReturnSuccess)
        {
            _displayLink = nil;
            return false;
        }

        if (CVDisplayLinkSetOutputCallback(_displayLink, OnTick, this) != kCVReturnSuccess)
        {
            CVDisplayLinkRelease(_displayLink);
            _displayLink = nil;
            return false;
        }

        return true;
    }

    static void OnDisplayReconfigured(CGDirectDisplayID, CGDisplayChangeSummaryFlags, void* userInfo)
    {
        // Reconfiguration callbacks may fire on a non-main thread; marshal to the main run
        // loop so the display link and _running are only touched on the thread that owns them.
        auto self = (PlatformRenderTimer*)userInfo;
        dispatch_async(dispatch_get_main_queue(), ^{ self->HandleReconfiguration(); });
    }

    void HandleReconfiguration()
    {
        @autoreleasepool
        {
            EnsureDisplayLink();
            if (_displayLink != nil && _running && CVDisplayLinkIsRunning(_displayLink) == false)
                CVDisplayLinkStart(_displayLink);

            // Let the managed timer re-query GetNeedsFallbackTimer and drop or resume its
            // software fallback now that the active display set has changed.
            if (_stateChanged != nullptr)
                _stateChanged->Run();
        }
    }

public:
    PlatformRenderTimer(IAvnActionCallback* tick, IAvnActionCallback* stateChanged)
        : _tick(tick), _stateChanged(stateChanged), _displayLink(nil), _running(false)
    {
        // Always observe display reconfiguration so we can recover a CVDisplayLink after a
        // hotplug or wake-from-sleep race and notify the managed timer to switch between the
        // hardware link and its software fallback.
        CGDisplayRegisterReconfigurationCallback(OnDisplayReconfigured, this);
        EnsureDisplayLink();
    }

    ~PlatformRenderTimer()
    {
        CGDisplayRemoveReconfigurationCallback(OnDisplayReconfigured, this);
        if (_displayLink != nil)
        {
            CVDisplayLinkStop(_displayLink);
            CVDisplayLinkRelease(_displayLink);
            _displayLink = nil;
        }
    }

    FORWARD_IUNKNOWN()

    virtual void Start () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            _running = true;
            if (_displayLink != nil && CVDisplayLinkIsRunning(_displayLink) == false)
                CVDisplayLinkStart(_displayLink);
        }
    }

    virtual void Stop () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            _running = false;
            if (_displayLink != nil && CVDisplayLinkIsRunning(_displayLink) == true)
                CVDisplayLinkStop(_displayLink);
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

    virtual int GetNeedsFallbackTimer () override
    {
        START_COM_CALL;

        @autoreleasepool
        {
            return _displayLink == nil ? 1 : 0;
        }
    }

    static CVReturn OnTick(CVDisplayLinkRef displayLink, const CVTimeStamp *inNow, const CVTimeStamp *inOutputTime, CVOptionFlags flagsIn, CVOptionFlags *flagsOut, void *displayLinkContext)
    {
        START_ARP_CALL;
        PlatformRenderTimer *object = (PlatformRenderTimer *)displayLinkContext;
        object->_tick->Run();
        return kCVReturnSuccess;
    }
};

extern IAvnPlatformRenderTimer* CreatePlatformRenderTimer(IAvnActionCallback* tick, IAvnActionCallback* stateChanged)
{
    return new PlatformRenderTimer(tick, stateChanged);
}
