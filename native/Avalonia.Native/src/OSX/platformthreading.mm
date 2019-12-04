// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"

class PlatformThreadingInterface;
@interface Signaler : NSObject
-(void) setParent: (PlatformThreadingInterface*)parent;
-(void) signal: (int) priority;
-(Signaler*) init;
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
    bool _wasRunningAtLeastOnce = false;
    
    class LoopCancellation : public ComSingleObject<IAvnLoopCancellation, &IID_IAvnLoopCancellation>
    {
    public:
        FORWARD_IUNKNOWN()
        bool Running = false;
        bool Cancelled = false;
        virtual void Cancel()
        {
            Cancelled = true;
            if(Running)
            {
                Running = false;
                dispatch_async(dispatch_get_main_queue(), ^{
                    [[NSApplication sharedApplication] stop:nil];
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
                });
            }
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
    
    virtual bool GetCurrentThreadIsLoopThread() override
    {
        return [[NSThread currentThread] isMainThread];
    }
    virtual void SetSignaledCallback(IAvnSignaledCallback* cb) override
    {
        SignaledCallback = cb;
    }
    virtual IAvnLoopCancellation* CreateLoopCancellation() override
    {
        return new LoopCancellation();
    }
    
    virtual HRESULT RunLoop(IAvnLoopCancellation* cancel) override
    {
        auto can = dynamic_cast<LoopCancellation*>(cancel);
        if(can->Cancelled)
            return S_OK;
        if(_wasRunningAtLeastOnce)
            return E_FAIL;
        can->Running = true;
        _wasRunningAtLeastOnce = true;
        [NSApp run];
        return S_OK;
    }
    
    virtual void Signal(int priority) override
    {
        [_signaler signal:priority];
    }
    
    virtual IUnknown* StartTimer(int priority, int ms, IAvnActionCallback* callback) override
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
