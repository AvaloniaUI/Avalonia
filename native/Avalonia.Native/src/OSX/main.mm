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
        
        
        /* carbon voodoo to get icon and menu without bundle */
        ProcessSerialNumber psn = { 0, kCurrentProcess };
        TransformProcessType(&psn, kProcessTransformToForegroundApplication);
        SetFrontProcess(&psn);
        
        id menubar = [NSMenu new];
        [NSApp setMainMenu:menubar];
        id appName = [[NSProcessInfo processInfo] processName];
        
        id fileMenu = [NSMenu new];
        [fileMenu setTitle:@"File"];
        
        id fileMenuItem = [NSMenuItem new];
        [[[NSApplication sharedApplication] mainMenu] addItem:fileMenuItem];
        [fileMenuItem setSubmenu:fileMenu];
        
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Generate Wallet" action:NULL keyEquivalent:@""]];
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Recover Wallet" action:NULL keyEquivalent:@""]];
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Load Wallet" action:NULL keyEquivalent:@""]];
        
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Open" action:NULL keyEquivalent:@""]];
        
        [fileMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Exit" action:NULL keyEquivalent:@""]];
        
        id toolsMenu = [NSMenu new];
        [toolsMenu setTitle:@"Tools"];
        
        id toolsMenuItem = [NSMenuItem new];
        [[[NSApplication sharedApplication] mainMenu] addItem:toolsMenuItem];
        [toolsMenuItem setSubmenu:toolsMenu];
        
        [toolsMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Wallet Manager" action:NULL keyEquivalent:@""]];
        [toolsMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Settings"     action:NULL keyEquivalent:@""]];
        
        id helpMenu = [NSMenu new];
        [helpMenu setTitle:@"Help"];
        
        id helpMenuItem = [NSMenuItem new];
        [[[NSApplication sharedApplication] mainMenu] addItem:helpMenuItem];
        [helpMenuItem setSubmenu:helpMenu];
        
        [helpMenu addItem:[[NSMenuItem alloc] initWithTitle:@"About" action:NULL keyEquivalent:@""]];
        [helpMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Customer Support" action:NULL keyEquivalent:@""]];
        
        [helpMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Report Bug" action:NULL keyEquivalent:@""]];
        [helpMenu addItem:[[NSMenuItem alloc] initWithTitle:@"FAQ" action:NULL keyEquivalent:@""]];
        [helpMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Privacy Policy" action:NULL keyEquivalent:@""]];
        [helpMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Terms and Conditions" action:NULL keyEquivalent:@""]];
        
        [helpMenu addItem:[[NSMenuItem alloc] initWithTitle:@"Legal Issues" action:NULL keyEquivalent:@""]];
        
        
        [[NSApplication sharedApplication] finishLaunching];
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
    
    virtual HRESULT ObtainMainAppMenu(IAvnAppMenu** ppv) override
    {
        return  S_OK;
    }
    
    virtual HRESULT CreateMenu (IAvnAppMenu** ppv) override
    {
        return S_OK;
    }
    
    virtual HRESULT CreateMenuItem (IAvnAppMenuItem** ppv) override
    {
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
