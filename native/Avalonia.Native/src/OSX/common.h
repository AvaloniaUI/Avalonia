#ifndef common_h
#define common_h
#include "comimpl.h"
#include "avalonia-native.h"
#include <stdio.h>
#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include <pthread.h>

extern IAvnPlatformThreadingInterface* CreatePlatformThreading();
extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events, IAvnGlContext* gl);
extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events, IAvnGlContext* gl);
extern IAvnSystemDialogs* CreateSystemDialogs();
extern IAvnScreens* CreateScreens();
extern IAvnClipboard* CreateClipboard();
extern IAvnCursorFactory* CreateCursorFactory();
extern IAvnGlDisplay* GetGlDisplay();
extern IAvnAppMenu* CreateAppMenu();
extern IAvnAppMenuItem* CreateAppMenuItem();
extern IAvnAppMenuItem* CreateAppMenuItemSeperator();
extern void SetAppMenu (NSString* appName, IAvnAppMenu* appMenu);
extern IAvnAppMenu* GetAppMenu ();
extern NSMenuItem* GetAppMenuItem ();

extern void InitializeAvnApp();
extern NSApplicationActivationPolicy AvnDesiredActivationPolicy;
extern NSPoint ToNSPoint (AvnPoint p);
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

#endif
