#ifndef common_h
#define common_h
#include "com.h"
#include "avalonia-native.h"
#include <stdio.h>
#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include <pthread.h>

extern IAvnPlatformThreadingInterface* CreatePlatformThreading();
extern IAvnWindow* CreateAvnWindow(IAvnWindowEvents*events);
extern IAvnPopup* CreateAvnPopup(IAvnWindowEvents*events);
extern IAvnSystemDialogs* CreateSystemDialogs();
extern IAvnScreens* CreateScreens();

extern NSPoint ToNSPoint (AvnPoint p);
extern AvnPoint ToAvnPoint (NSPoint p);
extern AvnPoint ConvertPointY (AvnPoint p);
#endif
