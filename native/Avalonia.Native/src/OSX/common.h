#ifndef common_h
#define common_h
#include "comimpl.h"
#include "avalonia-native.h"
#include <stdio.h>
#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include <pthread.h>
#include "noarc.h"

extern ComPtr<IAvnPlatformThreadingInterface> CreatePlatformThreading();
extern void FreeAvnGCHandle(void* handle);
extern void PostDispatcherCallback(const ComPtr<IAvnActionCallback>& cb);
extern ComPtr<IAvnTopLevel> CreateAvnTopLevel(const ComPtr<IAvnTopLevelEvents>& events);
extern ComPtr<IAvnWindow> CreateAvnWindow(const ComPtr<IAvnWindowEvents>& events);
extern ComPtr<IAvnPopup> CreateAvnPopup(const ComPtr<IAvnWindowEvents>& events);
extern ComPtr<IAvnStorageProvider> CreateStorageProvider();
extern ComPtr<IAvnScreens> CreateScreens(const ComPtr<IAvnScreenEvents>& cb);
extern ComPtr<IAvnClipboard> CreateClipboard(NSPasteboard* pb);
extern NSObject<NSDraggingSource>* CreateDraggingSource(NSDragOperation op, const ComPtr<IAvnDndResultCallback>& cb, void* handle);
extern void* GetAvnDataObjectHandleFromDraggingInfo(NSObject<NSDraggingInfo>* info);
extern NSString* GetAvnCustomDataType();
extern AvnDragDropEffects ConvertDragDropEffects(NSDragOperation nsop);
extern ComPtr<IAvnCursorFactory> CreateCursorFactory();
extern ComPtr<IAvnGlDisplay> GetGlDisplay();
extern ComPtr<IAvnMetalDisplay> GetMetalDisplay();
extern ComPtr<IAvnMenu> CreateAppMenu(const ComPtr<IAvnMenuEvents>& events);
extern ComPtr<IAvnTrayIcon> CreateTrayIcon();
extern ComPtr<IAvnMenuItem> CreateAppMenuItem();
extern ComPtr<IAvnMenuItem> CreateAppMenuItemSeparator();
extern ComPtr<IAvnApplicationCommands> CreateApplicationCommands();
extern ComPtr<IAvnPlatformBehaviorInhibition> CreatePlatformBehaviorInhibition();
extern ComPtr<IAvnNativeControlHost> CreateNativeControlHost(NSView* parent);
extern ComPtr<IAvnPlatformSettings> CreatePlatformSettings();
extern ComPtr<IAvnPlatformRenderTimer> CreatePlatformRenderTimer();
extern ComPtr<IAvnNativeObjectsMemoryManagement> CreateMemoryManagementHelper();
extern void SetAppMenu(const ComPtr<IAvnMenu>& menu);
extern void SetServicesMenu(const ComPtr<IAvnMenu>& menu);
class AvnAppMenu;
extern ComPtr<AvnAppMenu> GetAppMenu();
extern NSMenuItem* GetAppMenuItem ();
extern void SetDockMenu(NSMenu* menu);

extern void InitializeAvnApp(const ComPtr<IAvnApplicationEvents>& events, bool disableAppDelegate);
extern void ReleaseAvnAppEvents();
extern NSApplicationActivationPolicy AvnDesiredActivationPolicy;
extern NSPoint ToNSPoint (AvnPoint p);
extern NSRect ToNSRect (AvnRect r);
extern AvnPoint ToAvnPoint (NSPoint p);
extern AvnPoint ConvertPointY (AvnPoint p);
extern NSSize ToNSSize (AvnSize s);
extern AvnSize FromNSSize (NSSize s);
extern ComPtr<IAvnMTLSharedEvent> ImportMTLSharedEvent(void* object);
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
- (ActionCallback*) initWithCallback: (const ComPtr<IAvnActionCallback>&) callback;
- (void) action;
@end

@implementation NSScreen (AvNSScreen)
- (CGDirectDisplayID)av_displayId
{
    return [self.deviceDescription[@"NSScreenNumber"] unsignedIntValue];
}
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


#endif
