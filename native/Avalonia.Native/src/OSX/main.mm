// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
    FORWARD_IUNKNOWN()
    virtual HRESULT SetShowInDock(int show)  override
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
{
    int _fds[2];
}
- (void) runOnce
{
    char buf[]={0};
    write(_fds[1], buf, 1);
}

- (void) do
{
    pipe(_fds);
    [[[NSThread alloc] initWithTarget:self selector:@selector(runOnce) object:nil] start];
    char buf[1];
    read(_fds[0], buf, 1);
    close(_fds[0]);
    close(_fds[1]);
}


@end


class AvaloniaNative : public ComSingleObject<IAvaloniaNativeFactory, &IID_IAvaloniaNativeFactory>
{
    
public:
    FORWARD_IUNKNOWN()
    virtual HRESULT Initialize() override
    {
        @autoreleasepool{
            [[ThreadingInitializer new] do];
            return S_OK;
        }
    };
    
    virtual IAvnMacOptions* GetMacOptions()  override
    {
        return (IAvnMacOptions*)new MacOptions();
    }
    
    virtual HRESULT CreateWindow(IAvnWindowEvents* cb, IAvnWindow** ppv)  override
    {
        if(cb == nullptr || ppv == nullptr)
            return E_POINTER;
        *ppv = CreateAvnWindow(cb);
        return S_OK;
    };
    
    virtual HRESULT CreatePopup(IAvnWindowEvents* cb, IAvnPopup** ppv) override
    {
        if(cb == nullptr || ppv == nullptr)
            return E_POINTER;
        
        *ppv = CreateAvnPopup(cb);
        return S_OK;
    }
    
    virtual HRESULT CreatePlatformThreadingInterface(IAvnPlatformThreadingInterface** ppv)  override
    {
        *ppv = CreatePlatformThreading();
        return S_OK;
    }
    
    virtual HRESULT CreateSystemDialogs(IAvnSystemDialogs** ppv) override
    {
        *ppv = ::CreateSystemDialogs();
        return  S_OK;
    }
    
    virtual HRESULT CreateScreens (IAvnScreens** ppv) override
    {
        *ppv = ::CreateScreens ();
        return S_OK;
    }

    virtual HRESULT CreateClipboard(IAvnClipboard** ppv) override
    {
        *ppv = ::CreateClipboard ();
        return S_OK;
    }

    virtual HRESULT CreateCursorFactory(IAvnCursorFactory** ppv) override
    {
        *ppv = ::CreateCursorFactory();
        return S_OK;
    }
    
    virtual HRESULT ObtainGlFeature(IAvnGlFeature** ppv) override
    {
        auto rv = ::GetGlFeature();
        if(rv == NULL)
            return E_FAIL;
        rv->AddRef();
        *ppv = rv;
        return S_OK;
    }
};

extern "C" IAvaloniaNativeFactory* CreateAvaloniaNative()
{
    return new AvaloniaNative();
};

NSSize ToNSSize (AvnSize s)
{
    NSSize result;
    result.width = s.Width;
    result.height = s.Height;
    
    return result;
}

NSPoint ToNSPoint (AvnPoint p)
{
    NSPoint result;
    result.x = p.X;
    result.y = p.Y;
    
    return result;
}

AvnPoint ToAvnPoint (NSPoint p)
{
    AvnPoint result;
    result.X = p.x;
    result.Y = p.y;
    
    return result;
}

AvnPoint ConvertPointY (AvnPoint p)
{
    auto sw = [NSScreen.screens objectAtIndex:0].frame;
    
    auto t = MAX(sw.origin.y, sw.origin.y + sw.size.height);
    p.Y = t - p.Y;
    
    return p;
}
