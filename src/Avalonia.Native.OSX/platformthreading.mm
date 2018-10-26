// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"

class PlatformThreadingInterface;
@interface Signaler : NSObject
-(void) setParent: (PlatformThreadingInterface*)parent;
-(void) signal: (int) priority;
-(Signaler*) init;
@end


@interface ActionCallback : NSObject
- (ActionCallback*) initWithCallback: (IAvnActionCallback*) callback;
- (void) action;
@end

@implementation ActionCallback
{
    ComPtr<IAvnActionCallback> _callback;

}
- (ActionCallback*) initWithCallback: (IAvnActionCallback*) callback
{
    _callback = callback;
    return self;
}

- (void) action
{
    _callback->Run();
}


@end

class TimerWrapper : public ComUnknownObject
{
    NSTimer* _timer;
public:
    TimerWrapper(IAvnActionCallback* callback, int ms)
    {
        auto cb = [[ActionCallback alloc] initWithCallback:callback];
        _timer = [NSTimer scheduledTimerWithTimeInterval:(NSTimeInterval)(double)ms/1000 target:cb selector:@selector(action) userInfo:nullptr repeats:true];
    }
                  
    virtual ~TimerWrapper()
    {
         [_timer invalidate];
    }
};



class PlatformThreadingInterface : public ComSingleObject<IAvnPlatformThreadingInterface, &IID_IAvnPlatformThreadingInterface>
{
private:
    Signaler* _signaler;
    
    class LoopCancellation : public ComSingleObject<IAvnLoopCancellation, &IID_IAvnLoopCancellation>
    {
    public:
        FORWARD_IUNKNOWN()
        bool Cancelled = 0;
        virtual void Cancel()
        {
            Cancelled = 1;
        }
    };
    
public:
    FORWARD_IUNKNOWN()
    ComPtr<IAvnSignaledCallback> SignaledCallback;

    PlatformThreadingInterface()
    {
        _signaler = [Signaler new];
        [_signaler setParent:this];
    }
    
    ~PlatformThreadingInterface()
    {
        if(_signaler)
            [_signaler setParent: NULL];
        _signaler = NULL;
    }
    
    virtual bool GetCurrentThreadIsLoopThread()
    {
        return [[NSThread currentThread] isMainThread];
    }
    virtual void SetSignaledCallback(IAvnSignaledCallback* cb)
    {
        SignaledCallback = cb;
    }
    virtual IAvnLoopCancellation* CreateLoopCancellation()
    {
        return new LoopCancellation();
    }
    
    virtual void RunLoop(IAvnLoopCancellation* cancel)
    {
        @autoreleasepool {
            auto can = dynamic_cast<LoopCancellation*>(cancel);
            [[NSApplication sharedApplication] activateIgnoringOtherApps:true];
            while(true)
            {
                @autoreleasepool
                {
                    if(can != NULL && can->Cancelled)
                        return;
                    NSEvent* ev = [[NSApplication sharedApplication]
                                   nextEventMatchingMask:NSEventMaskAny
                                   untilDate: [NSDate dateWithTimeIntervalSinceNow:1]
                                   inMode:NSDefaultRunLoopMode
                                   dequeue:true];
                    if(can != NULL && can->Cancelled)
                        return;
                    if(ev != NULL)
                        [[NSApplication sharedApplication] sendEvent:ev];
                }
            }
            NSDebugLog(@"RunLoop exited");
        }
    }
    
    virtual void Signal(int priority)
    {
        [_signaler signal:priority];
    }
    
    virtual IUnknown* StartTimer(int priority, int ms, IAvnActionCallback* callback)
    {
        @autoreleasepool {
            
            return new TimerWrapper(callback, ms);
        }
    }
};

@implementation Signaler

PlatformThreadingInterface* _parent = 0;
bool _signaled = 0;
NSArray<NSString*>* _modes;

-(Signaler*) init
{
    if(self = [super init])
    {
        _modes = [NSArray arrayWithObjects: NSDefaultRunLoopMode, NSEventTrackingRunLoopMode, NSModalPanelRunLoopMode, NSRunLoopCommonModes, NSConnectionReplyMode, nil];
    }
    return self;
}

-(void) perform
{
    @synchronized (self) {
        _signaled  = false;
        if(_parent != NULL && _parent->SignaledCallback != NULL)
            _parent->SignaledCallback->Signaled(0, false);
    }
}

-(void) setParent:(PlatformThreadingInterface *)parent
{
    @synchronized (self) {
        _parent = parent;
    }
}

-(void) signal: (int) priority
{

    @synchronized (self) {
        if(_signaled)
            return;
        _signaled = true;
        [self performSelector:@selector(perform) onThread:[NSThread mainThread] withObject:NULL waitUntilDone:false modes:_modes];
    }
    
}
@end


extern IAvnPlatformThreadingInterface* CreatePlatformThreading()
{
    return new PlatformThreadingInterface();
}
