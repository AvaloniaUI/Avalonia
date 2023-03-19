#include "common.h"

class PlatformThreadingInterface;


class LoopCancellation : public ComSingleObject<IAvnLoopCancellation, &IID_IAvnLoopCancellation>
{
public:
    FORWARD_IUNKNOWN()
    
    bool Running = false;
    bool Cancelled = false;
    bool IsApp = false;

    virtual void Cancel() override
    {
        Cancelled = true;
        if(Running)
        {
            Running = false;
            if(![NSThread isMainThread])
            {
                AddRef();
                dispatch_async(dispatch_get_main_queue(), ^{
                    if(Release() == 0)
                        return;
                    Cancel();
                });
                return;
            };
            if(IsApp)
                [NSApp stop:nil];
            else
            {
                // Wakeup the event loop
                NSEvent* event = [NSEvent otherEventWithType:NSEventTypeApplicationDefined
                                                    location:NSMakePoint(0, 0)
                                               modifierFlags:0
                                                   timestamp:0
                                                windowNumber:0
                                                     context:nil
                                                     subtype:0
                                                       data1:0
                                                       data2:0];
                [NSApp postEvent:event atStart:YES];
            }
        }
    };
};

// CFRunLoopTimerSetNextFireDate docs recommend to "create a repeating timer with an initial
// firing time in the distant future (or the initial firing time) and a very large repeat
// interval—on the order of decades or more"
static double distantFutureInterval = (double)50*365*24*3600;

@interface Signaler : NSObject
-(void) setEvents:(IAvnPlatformThreadingInterfaceEvents*) events;
-(void) updateTimer:(int)ms;
-(Signaler*) init;
-(void) destroyObserver;
-(void) signal;
@end

@implementation Signaler
{
    ComPtr<IAvnPlatformThreadingInterfaceEvents> _events;
    bool _wakeupDelegateSent;
    bool _signaled;
    CFRunLoopObserverRef _observer;
    CFRunLoopTimerRef _timer;
}

- (Signaler*) init
{
    _observer = CFRunLoopObserverCreateWithHandler(nil,
                                                   kCFRunLoopBeforeSources | kCFRunLoopAfterWaiting,
                                                   true, 0,
                                                   ^(CFRunLoopObserverRef observer, CFRunLoopActivity activity) {
        bool signaled;
        @synchronized (self) {
            signaled = self->_signaled;
            self->_signaled = false;
        }
        if(signaled)
        {
            self->_events->Signaled();
        }
    });
    CFRunLoopAddObserver(CFRunLoopGetMain(), _observer, kCFRunLoopCommonModes);
    
    
    _timer = CFRunLoopTimerCreateWithHandler(nil, CFAbsoluteTimeGetCurrent() + distantFutureInterval, distantFutureInterval, 0, 0, ^(CFRunLoopTimerRef timer) {
        self->_events->Timer();
    });
    
    CFRunLoopAddTimer(CFRunLoopGetMain(), _timer, kCFRunLoopCommonModes);
    
    return self;
}

- (void) destroyObserver
{
    if(_observer != nil)
    {
        CFRunLoopObserverInvalidate(_observer);
        CFRelease(_observer);
        _observer = nil;
    }
    
    if(_timer != nil)
    {
        CFRunLoopTimerInvalidate(_timer);
        CFRelease(_timer);
        _timer = nil;
    }
}

-(void) updateTimer:(int)ms
{
    if(_timer == nil)
        return;
    double interval = ms < 0 ? distantFutureInterval : ((double)ms / 1000);
    CFRunLoopTimerSetTolerance(_timer, 0);
    CFRunLoopTimerSetNextFireDate(_timer, CFAbsoluteTimeGetCurrent() + interval);
}

- (void) setEvents: (IAvnPlatformThreadingInterfaceEvents*) events
{
    _events = events;
}

- (void) signal
{
    @synchronized (self) {
        if(_signaled)
            return;
        _signaled = true;
        CFRunLoopWakeUp(CFRunLoopGetMain());
    }
}

@end


class PlatformThreadingInterface : public ComSingleObject<IAvnPlatformThreadingInterface, &IID_IAvnPlatformThreadingInterface>
{
private:
    ComPtr<IAvnPlatformThreadingInterfaceEvents> _events;
    Signaler* _signaler;
    CFRunLoopObserverRef _observer = nil;
public:
    FORWARD_IUNKNOWN()
    PlatformThreadingInterface()
    {
        _signaler = [Signaler new];
    };
    
    ~PlatformThreadingInterface()
    {
        [_signaler destroyObserver];
    }
    
    bool GetCurrentThreadIsLoopThread() override
    {
        return [NSThread isMainThread];
    };
    
    bool HasPendingInput() override
    {
        auto event = [NSApp
                      nextEventMatchingMask: NSEventMaskAny
                      untilDate:nil
                      inMode:NSDefaultRunLoopMode
                      dequeue:false];
        return event != nil;
    };
    
    void SetEvents(IAvnPlatformThreadingInterfaceEvents *cb) override
    {
        _events = cb;
        [_signaler setEvents:cb];
    };
    
    IAvnLoopCancellation *CreateLoopCancellation() override
    {
        return new LoopCancellation();
    };
    
    void RunLoop(IAvnLoopCancellation *cancel) override
    {
        START_COM_CALL;
        auto can = dynamic_cast<LoopCancellation*>(cancel);
        if(can->Cancelled)
            return;
        can->Running = true;
        if(![NSApp isRunning])
        {
            can->IsApp = true;
            [NSApp run];
            return;
        }
        else
        {
            while(!can->Cancelled)
            {
                @autoreleasepool
                {
                    NSEvent* ev = [NSApp
                                   nextEventMatchingMask:NSEventMaskAny
                                   untilDate: [NSDate dateWithTimeIntervalSinceNow:1]
                                   inMode:NSDefaultRunLoopMode
                                   dequeue:true];
                    if(ev != NULL)
                        [NSApp sendEvent:ev];
                }
            }
        }
    };
    
    void Signal() override
    {
        [_signaler signal];
    };
    
    void UpdateTimer(int ms) override
    {
        [_signaler updateTimer:ms];
    };
    
};

extern IAvnPlatformThreadingInterface* CreatePlatformThreading()
{
    return new PlatformThreadingInterface();
}
