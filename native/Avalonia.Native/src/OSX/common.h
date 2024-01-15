#ifndef common_h
#define common_h
#include "comimpl.h"
#include "avalonia-native.h"
#include <stdio.h>
#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include <pthread.h>
#include "noarc.h"

extern IAvnPlatformThreadingInterface* CreatePlatformThreading();
extern void FreeAvnGCHandle(void* handle);
extern void PostDispatcherCallback(IAvnActionCallback* cb);
extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events);
extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events);
extern IAvnSystemDialogs* CreateSystemDialogs();
extern IAvnScreens* CreateScreens();
extern IAvnClipboard* CreateClipboard(NSPasteboard*, NSPasteboardItem*);
extern NSPasteboardItem* TryGetPasteboardItem(IAvnClipboard*);
extern NSObject<NSDraggingSource>* CreateDraggingSource(NSDragOperation op, IAvnDndResultCallback* cb, void* handle);
extern void* GetAvnDataObjectHandleFromDraggingInfo(NSObject<NSDraggingInfo>* info);
extern NSString* GetAvnCustomDataType();
extern AvnDragDropEffects ConvertDragDropEffects(NSDragOperation nsop);
extern IAvnCursorFactory* CreateCursorFactory();
extern IAvnGlDisplay* GetGlDisplay();
extern IAvnMetalDisplay* GetMetalDisplay();
extern IAvnMenu* CreateAppMenu(IAvnMenuEvents* events);
extern IAvnTrayIcon* CreateTrayIcon();
extern IAvnMenuItem* CreateAppMenuItem();
extern IAvnMenuItem* CreateAppMenuItemSeparator();
extern IAvnApplicationCommands* CreateApplicationCommands();
extern IAvnPlatformBehaviorInhibition* CreatePlatformBehaviorInhibition();
extern IAvnNativeControlHost* CreateNativeControlHost(NSView* parent);
extern IAvnPlatformSettings* CreatePlatformSettings();
extern IAvnPlatformRenderTimer* CreatePlatformRenderTimer();
extern void SetAppMenu(IAvnMenu *menu);
extern void SetServicesMenu (IAvnMenu* menu);
extern IAvnMenu* GetAppMenu ();
extern NSMenuItem* GetAppMenuItem ();

extern void InitializeAvnApp(IAvnApplicationEvents* events, bool disableAppDelegate);
extern void ReleaseAvnAppEvents();
extern NSApplicationActivationPolicy AvnDesiredActivationPolicy;
extern NSPoint ToNSPoint (AvnPoint p);
extern NSRect ToNSRect (AvnRect r);
extern AvnPoint ToAvnPoint (NSPoint p);
extern AvnPoint ConvertPointY (AvnPoint p);
extern NSSize ToNSSize (AvnSize s);
#ifdef DEBUG
#define NSDebugLog(...) NSLog(__VA_ARGS__)
#else
#define NSDebugLog(...) (void)0
#endif

template<typename T> inline T* objc_cast(id from) {
    if(from == nil)
        return nil;
    if ([from isKindOfClass:[T class]]) {
        return static_cast<T*>(from);
    }
    return nil;
}

template<typename T> class ObjCWrapper {
public:
    T* Value;
    ObjCWrapper(T* value)
    {
        Value = value;
    }
    operator T*() const
    {
        return Value;
    }
    T* operator->() const
    {
        return Value;
    }
    ~ObjCWrapper()
    {
        Value = nil;
    }
};

@interface ActionCallback : NSObject
- (ActionCallback*) initWithCallback: (IAvnActionCallback*) callback;
- (void) action;
@end

class AvnInsidePotentialDeadlock
{
public:
    static bool IsInside();
    AvnInsidePotentialDeadlock();
    ~AvnInsidePotentialDeadlock();
};


class AvnApplicationCommands : public ComSingleObject<IAvnApplicationCommands, &IID_IAvnApplicationCommands>
{
public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT UnhideApp() override;
    virtual HRESULT HideApp() override;
    virtual HRESULT ShowAll() override;
    virtual HRESULT HideOthers() override;
};
#define NSApp [NSApplication sharedApplication]

#define START_COM_ARP_CALL START_ARP_CALL; START_COM_CALL

#endif
