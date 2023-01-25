#ifndef common_h
#define common_h
#include "comimpl.h"
#include "avalonia-native.h"
#include <stdio.h>
#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include <pthread.h>

extern IAvnPlatformThreadingInterface* CreatePlatformThreading();
extern void FreeAvnGCHandle(void* handle);
extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events, IAvnGlContext* gl);
extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events, IAvnGlContext* gl);
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
extern IAvnMenu* CreateAppMenu(IAvnMenuEvents* events);
extern IAvnTrayIcon* CreateTrayIcon();
extern IAvnMenuItem* CreateAppMenuItem();
extern IAvnMenuItem* CreateAppMenuItemSeparator();
extern IAvnApplicationCommands* CreateApplicationCommands();
extern IAvnNativeControlHost* CreateNativeControlHost(NSView* parent);
extern IAvnPlatformSettings* CreatePlatformSettings();
extern void SetAppMenu(IAvnMenu *menu);
extern void SetServicesMenu (IAvnMenu* menu);
extern IAvnMenu* GetAppMenu ();
extern NSMenuItem* GetAppMenuItem ();

extern void InitializeAvnApp(IAvnApplicationEvents* events, bool disableAppDelegate);
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
    
    virtual HRESULT HideApp() override;
    virtual HRESULT ShowAll() override;
    virtual HRESULT HideOthers() override;
};

#endif
