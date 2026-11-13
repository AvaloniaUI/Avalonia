//This file will contain actual IID structures
#define COM_GUIDS_MATERIALIZE
#include "common.h"
#include "menu.h"

static NSString* s_appTitle = @"Avalonia";
static int disableSetProcessName = 0;
static bool disableAppDelegate = false;

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
    
    ls_set_application_information_item_func(magic_session_constant, asn,
                                             ls_display_name_key,
                                             process_name,
                                             NULL /* optional out param */);
}

class MacOptions : public ComSingleObject<IAvnMacOptions, &IID_IAvnMacOptions>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT SetApplicationTitle(char* utf8String) override
    {
        @autoreleasepool
        {
            auto appTitle = [NSString stringWithUTF8String: utf8String];
            if (disableSetProcessName == 0)
            {
                [[NSProcessInfo processInfo] setProcessName:appTitle];
                
                SetProcessName(appTitle);
            }
            if (disableSetProcessName == 1)
            {
                auto rootMenu = [NSApp mainMenu];
                [rootMenu setTitle:appTitle];
            }
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetShowInDock(int show) override
    {
        @autoreleasepool
        {
            NSApplication* app = [NSApplication sharedApplication];
            NSApplicationActivationPolicy requestedPolicy = show
                ? NSApplicationActivationPolicyRegular
                : NSApplicationActivationPolicyAccessory;
            
            if ([app activationPolicy] != requestedPolicy)
            {
                [app setActivationPolicy:requestedPolicy];
            }
            
            return S_OK;
        }
    }
    
    virtual HRESULT SetDisableSetProcessName(int disable)  override
    {
        @autoreleasepool
        {
            disableSetProcessName = disable;
            return S_OK;
        }
    }
    
    virtual HRESULT SetDisableAppDelegate(int disable) override
    {
        @autoreleasepool {
            disableAppDelegate = disable;
            return S_OK;
        }
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

// Not released at exit: .NET is already shut down by then
static ComStaticPtr<IAvnGCHandleDeallocatorCallback> _deallocator;
static ComStaticPtr<IAvnDispatcher> _dispatcher;
class AvaloniaNative : public ComSingleObject<IAvaloniaNativeFactory, &IID_IAvaloniaNativeFactory>
{
    
public:
    FORWARD_IUNKNOWN()
    
    virtual ~AvaloniaNative() override
    {
        ReleaseAvnAppEvents();
        _deallocator.reset();
        _dispatcher.reset();
    }
    
    virtual HRESULT Initialize(const ComPtr<IAvnGCHandleDeallocatorCallback>& deallocator,
            const ComPtr<IAvnApplicationEvents>& events,
            const ComPtr<IAvnDispatcher>& dispatcher) override
    {
        _deallocator.set(deallocator);
        _dispatcher.set(dispatcher);
        @autoreleasepool{
            [[ThreadingInitializer new] do];
        }
        InitializeAvnApp(events, disableAppDelegate);
        return S_OK;
    };
    
    virtual ComPtr<IAvnMacOptions> GetMacOptions() override
    {
        return comnew<MacOptions>();
    }
    
    virtual HRESULT CreateTopLevel(const ComPtr<IAvnTopLevelEvents>& cb, ComPtr<IAvnTopLevel>& ppv) override
    {
        if(cb == nullptr)
            return E_POINTER;
        ppv = CreateAvnTopLevel(cb);
        return S_OK;
    }
    
    virtual HRESULT CreateWindow(const ComPtr<IAvnWindowEvents>& cb, ComPtr<IAvnWindow>& ppv) override
    {
        if(cb == nullptr)
            return E_POINTER;
        ppv = CreateAvnWindow(cb);
        return S_OK;
    };
    
    virtual HRESULT CreatePopup(const ComPtr<IAvnWindowEvents>& cb, ComPtr<IAvnPopup>& ppv) override
    {
        if(cb == nullptr)
            return E_POINTER;
        ppv = CreateAvnPopup(cb);
        return S_OK;
    }
    
    virtual HRESULT CreatePlatformThreadingInterface(ComPtr<IAvnPlatformThreadingInterface>& ppv) override
    {
        ppv = CreatePlatformThreading();
        return S_OK;
    }
    
    virtual HRESULT CreateStorageProvider(ComPtr<IAvnStorageProvider>& ppv) override
    {
        ppv = ::CreateStorageProvider();
        return S_OK;
    }
    
    virtual HRESULT CreateScreens(const ComPtr<IAvnScreenEvents>& cb, ComPtr<IAvnScreens>& ppv) override
    {
        ppv = ::CreateScreens(cb);
        return S_OK;
    }

    virtual HRESULT CreateClipboard(ComPtr<IAvnClipboard>& ppv) override
    {
        ppv = ::CreateClipboard(nil);
        return S_OK;
    }

    virtual HRESULT CreateCursorFactory(ComPtr<IAvnCursorFactory>& ppv) override
    {
        ppv = ::CreateCursorFactory();
        return S_OK;
    }
    
    virtual HRESULT ObtainGlDisplay(ComPtr<IAvnGlDisplay>& ppv) override
    {
        ppv = ::GetGlDisplay();
        return ppv ? S_OK : E_FAIL;
    }

    virtual HRESULT ObtainMetalDisplay(ComPtr<IAvnMetalDisplay>& ppv) override
    {
        ppv = ::GetMetalDisplay();
        return ppv ? S_OK : E_FAIL;
    }

    virtual HRESULT CreateTrayIcon(ComPtr<IAvnTrayIcon>& ppv) override
    {
        ppv = ::CreateTrayIcon();
        return S_OK;
    }
    
    virtual HRESULT CreateMenu(const ComPtr<IAvnMenuEvents>& cb, ComPtr<IAvnMenu>& ppv) override
    {
        ppv = ::CreateAppMenu(cb);
        return S_OK;
    }
    
    virtual HRESULT CreateMenuItem(ComPtr<IAvnMenuItem>& ppv) override
    {
        ppv = ::CreateAppMenuItem();
        return S_OK;
    }
    
    virtual HRESULT CreateMenuItemSeparator(ComPtr<IAvnMenuItem>& ppv) override
    {
        ppv = ::CreateAppMenuItemSeparator();
        return S_OK;
    }
        
    virtual HRESULT SetAppMenu(const ComPtr<IAvnMenu>& appMenu) override
    {
        ::SetAppMenu(appMenu);
        return S_OK;
    }
    
    virtual HRESULT SetServicesMenu(const ComPtr<IAvnMenu>& servicesMenu) override
    {
        ::SetServicesMenu(servicesMenu);
        return S_OK;
    }
    
    virtual HRESULT CreateApplicationCommands(ComPtr<IAvnApplicationCommands>& ppv) override
    {
        ppv = ::CreateApplicationCommands();
        return S_OK;
    }
    
    virtual HRESULT CreatePlatformSettings(ComPtr<IAvnPlatformSettings>& ppv) override
    {
        ppv = ::CreatePlatformSettings();
        return S_OK;
    }

    virtual HRESULT CreatePlatformBehaviorInhibition(ComPtr<IAvnPlatformBehaviorInhibition>& ppv) override
    {
        ppv = ::CreatePlatformBehaviorInhibition();
        return S_OK;
    }
    
    virtual HRESULT CreatePlatformRenderTimer(ComPtr<IAvnPlatformRenderTimer>& ppv) override
    {
        ppv = ::CreatePlatformRenderTimer();
        return S_OK;
    }
    
    virtual HRESULT ImportMTLSharedEvent(void* event, ComPtr<IAvnMTLSharedEvent>& ppv) override
    {
        ppv = ::ImportMTLSharedEvent(event);
        return ppv ? S_OK : E_FAIL;
    }
    
    virtual HRESULT CreateMemoryManagementHelper(ComPtr<IAvnNativeObjectsMemoryManagement>& ppv) override
    {
        ppv = ::CreateMemoryManagementHelper();
        return S_OK;
    }

    virtual HRESULT SetDockMenu(const ComPtr<IAvnMenu>& dockMenu) override
    {
        auto nativeMenu = dockMenu.dynamicCast<AvnAppMenu>();
        ::SetDockMenu(nativeMenu ? nativeMenu->GetNative() : nil);
        return S_OK;
    }

};

// C ABI entry point: the caller takes ownership of the returned reference
extern "C" IAvaloniaNativeFactory* CreateAvaloniaNative()
{
    return comnew<AvaloniaNative>().getRetainedReference();
};

extern void FreeAvnGCHandle(void* handle)
{
    if(_deallocator)
        _deallocator->FreeGCHandle(handle);
}

extern void PostDispatcherCallback(const ComPtr<IAvnActionCallback>& cb)
{
    _dispatcher->Post(cb);
}

NSSize ToNSSize (AvnSize s)
{
    NSSize result;
    result.width = s.Width;
    result.height = s.Height;
    
    return result;
}

AvnSize FromNSSize (NSSize s)
{
    AvnSize result;
    result.Width = s.width;
    result.Height = s.height;
    
    return result;
}

NSPoint ToNSPoint (AvnPoint p)
{
    NSPoint result;
    result.x = p.X;
    result.y = p.Y;
    
    return result;
}

NSRect ToNSRect (AvnRect r)
{
    return NSRect
    {
        NSPoint { r.X, r.Y },
        NSSize { r.Width, r.Height }
    };
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
    auto primaryDisplayHeight = NSMaxY([[[NSScreen screens] firstObject] frame]);
    
    p.Y = primaryDisplayHeight - p.Y;
    
    return p;
}

