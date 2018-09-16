//This file will contain actual IID structures
#define COM_GUIDS_MATERIALIZE
#include "common.h"

static BOOL ShowInDock = 1;

static void SetActivationPolicy()
{
    [[NSApplication sharedApplication] setActivationPolicy: (ShowInDock ? NSApplicationActivationPolicyRegular : NSApplicationActivationPolicyAccessory)];
}

class MacOptions : public ComSingleObject<IAvnMacOptions, &IID_IAvnMacOptions>
{
public:
    virtual HRESULT SetShowInDock(int show)
    {
        ShowInDock = show;
        SetActivationPolicy();
        return S_OK;
    }
};



/// See "Using POSIX Threads in a Cocoa Application" section here:
/// https://developer.apple.com/library/content/documentation/Cocoa/Conceptual/Multithreading/CreatingThreads/CreatingThreads.html#//apple_ref/doc/uid/20000738-125024
@interface ThreadingInitializer : NSObject
- (void) do;
@end
@implementation ThreadingInitializer

pthread_mutex_t mutex;
pthread_cond_t cond;

- (void) runOnce
{
    pthread_mutex_lock(&mutex);
    pthread_cond_signal(&cond);
    pthread_mutex_unlock(&mutex);
}

- (void) do
{
    pthread_mutex_init(&mutex, NULL);
    pthread_cond_init(&cond, NULL);
    [[[NSThread alloc] initWithTarget:self selector:@selector(runOnce) object:nil] start];
    pthread_mutex_lock(&mutex);
    pthread_cond_wait(&cond, &mutex);
    pthread_mutex_unlock(&mutex);
    pthread_cond_destroy(&cond);
    pthread_mutex_destroy(&mutex);
}


@end


class AvaloniaNative : public ComSingleObject<IAvaloniaNativeFactory, &IID_IAvaloniaNativeFactory>
{
    
public:
    virtual HRESULT Initialize()
    {
        @autoreleasepool{
            [[ThreadingInitializer new] do];
            return S_OK;
        }
    };
    
    virtual IAvnMacOptions* GetMacOptions()
    {
        return (IAvnMacOptions*)new MacOptions();
    }
    
    virtual HRESULT CreateWindow(IAvnWindowEvents* cb, IAvnWindow** ppv)
    {
        if(cb == nullptr || ppv == nullptr)
            return E_POINTER;
        *ppv = CreateAvnWindow(cb);
        return S_OK;
    };
    
    virtual HRESULT CreatePopup(IAvnWindowEvents* cb, IAvnPopup** ppv)
    {
        if(cb == nullptr || ppv == nullptr)
            return E_POINTER;
        
        *ppv = CreateAvnPopup(cb);
        return S_OK;
    }
    
    virtual HRESULT CreatePlatformThreadingInterface(IAvnPlatformThreadingInterface** ppv)
    {
        *ppv = CreatePlatformThreading();
        return S_OK;
    }
};

extern "C" IAvaloniaNativeFactory* CreateAvaloniaNative()
{
    return new AvaloniaNative();
};
