// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

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
extern IAvnClipboard* CreateClipboard();
extern IAvnCursorFactory* CreateCursorFactory();

extern NSPoint ToNSPoint (AvnPoint p);
extern AvnPoint ToAvnPoint (NSPoint p);
extern AvnPoint ConvertPointY (AvnPoint p);
extern NSSize ToNSSize (AvnSize s);
#endif
