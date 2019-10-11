// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

//This file will contain actual IID structures
#define COM_GUIDS_MATERIALIZE
#include "common.h"

static NSString* s_appTitle = @"Avalonia";

// Copyright (c) 2011 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.
void SetProcessName(NSString* appTitle) {
    s_appTitle = appTitle;
    
    CFStringRef process_name = (__bridge CFStringRef)appTitle;
    
    if (!process_name || CFStringGetLength(process_name) == 0) {
        //NOTREACHED() << "SetProcessName given bad name.";
        return;
    }
    
    if (![NSThread isMainThread]) {
        //NOTREACHED() << "Should only set process name from main thread.";
        return;
    }
    
    // Warning: here be dragons! This is SPI reverse-engineered from WebKit's
    // plugin host, and could break at any time (although realistically it's only
    // likely to break in a new major release).
    // When 10.7 is available, check that this still works, and update this
    // comment for 10.8.
    
    // Private CFType used in these LaunchServices calls.
    typedef CFTypeRef PrivateLSASN;
    typedef PrivateLSASN (*LSGetCurrentApplicationASNType)();
    typedef OSStatus (*LSSetApplicationInformationItemType)(int, PrivateLSASN,
                                                            CFStringRef,
                                                            CFStringRef,
                                                            CFDictionaryRef*);
    
    static LSGetCurrentApplicationASNType ls_get_current_application_asn_func =
    NULL;
    static LSSetApplicationInformationItemType
    ls_set_application_information_item_func = NULL;
    static CFStringRef ls_display_name_key = NULL;
    
    static bool did_symbol_lookup = false;
    if (!did_symbol_lookup) {
        did_symbol_lookup = true;
        CFBundleRef launch_services_bundle =
        CFBundleGetBundleWithIdentifier(CFSTR("com.apple.LaunchServices"));
        if (!launch_services_bundle) {
            //LOG(ERROR) << "Failed to look up LaunchServices bundle";
            return;
        }
        
        ls_get_current_application_asn_func =
        reinterpret_cast<LSGetCurrentApplicationASNType>(
                                                         CFBundleGetFunctionPointerForName(
                                                                                           launch_services_bundle, CFSTR("_LSGetCurrentApplicationASN")));
        if (!ls_get_current_application_asn_func){}
        //LOG(ERROR) << "Could not find _LSGetCurrentApplicationASN";
        
        ls_set_application_information_item_func =
        reinterpret_cast<LSSetApplicationInformationItemType>(
                                                              CFBundleGetFunctionPointerForName(
                                                                                                launch_services_bundle,
                                                                                                CFSTR("_LSSetApplicationInformationItem")));
        if (!ls_set_application_information_item_func){}
        //LOG(ERROR) << "Could not find _LSSetApplicationInformationItem";
        
        CFStringRef* key_pointer = reinterpret_cast<CFStringRef*>(
                                                                  CFBundleGetDataPointerForName(launch_services_bundle,
                                                                                                CFSTR("_kLSDisplayNameKey")));
        ls_display_name_key = key_pointer ? *key_pointer : NULL;
        if (!ls_display_name_key){}
        //LOG(ERROR) << "Could not find _kLSDisplayNameKey";
        
        // Internally, this call relies on the Mach ports that are started up by the
        // Carbon Process Manager.  In debug builds this usually happens due to how
        // the logging layers are started up; but in release, it isn't started in as
        // much of a defined order.  So if the symbols had to be loaded, go ahead
        // and force a call to make sure the manager has been initialized and hence
        // the ports are opened.
        ProcessSerialNumber psn;
        GetCurrentProcess(&psn);
    }
    if (!ls_get_current_application_asn_func ||
        !ls_set_application_information_item_func ||
        !ls_display_name_key) {
        return;
    }
    
    PrivateLSASN asn = ls_get_current_application_asn_func();
    // Constant used by WebKit; what exactly it means is unknown.
    const int magic_session_constant = -2;
    OSErr err =
    ls_set_application_information_item_func(magic_session_constant, asn,
                                             ls_display_name_key,
                                             process_name,
                                             NULL /* optional out param */);
    //LOG_IF(ERROR, err) << "Call to set process name failed, err " << err;
}

class MacOptions : public ComSingleObject<IAvnMacOptions, &IID_IAvnMacOptions>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT SetApplicationTitle(void* utf8String) override
    {
        auto appTitle = [NSString stringWithUTF8String:(const char*)utf8String];
        
        [[NSProcessInfo processInfo] setProcessName:appTitle];
        
        
        SetProcessName(appTitle);
        
        return S_OK;
    }
    
    virtual HRESULT SetShowInDock(int show)  override
    {
        AvnDesiredActivationPolicy = show
            ? NSApplicationActivationPolicyRegular : NSApplicationActivationPolicyAccessory;
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
        }
        InitializeAvnApp();
        return S_OK;
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
    
    virtual HRESULT CreateMenu (IAvnAppMenu** ppv) override
    {
        *ppv = ::CreateAppMenu();
        return S_OK;
    }
    
    virtual HRESULT CreateMenuItem (IAvnAppMenuItem** ppv) override
    {
        *ppv = ::CreateAppMenuItem();
        return S_OK;
    }
    
    virtual HRESULT CreateMenuItemSeperator (IAvnAppMenuItem** ppv) override
    {
        *ppv = ::CreateAppMenuItemSeperator();
        return S_OK;
    }
    
    virtual HRESULT SetAppMenu (IAvnAppMenu* appMenu) override
    {
        ::SetAppMenu(s_appTitle, appMenu);
        return S_OK;
    }
    
    virtual HRESULT ObtainAppMenu(IAvnAppMenu** retOut) override
    {
        if(retOut == nullptr)
        {
            return E_POINTER;
        }
        
        *retOut = ::GetAppMenu();
        
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
