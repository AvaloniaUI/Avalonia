//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include "WindowProtocol.h"
#include "AvnPopup.h"
#include "WindowBaseImpl.h"

@interface AvnWindow : NSWindow <AvnWindowProtocol, NSWindowDelegate>
-(AvnWindow* _Nonnull) initWithWindowImpl: (WindowBaseImpl* _Nonnull) windowImpl contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
@end

@interface AvnPopup : NSWindow <AvnPopupProtocol, NSWindowDelegate>
-(AvnPopup* _Nonnull) initWithWindowImpl: (WindowBaseImpl* _Nonnull) windowImpl contentRect: (NSRect)contentRect;
@end

@interface AvnPanel : NSPanel <AvnWindowProtocol, NSWindowDelegate>
-(AvnPanel* _Nonnull) initWithWindowImpl: (WindowBaseImpl* _Nonnull) windowImpl contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
@end
