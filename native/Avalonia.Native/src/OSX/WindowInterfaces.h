//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include "WindowProtocol.h"
#include "WindowBaseImpl.h"
#include "AvnAccessibility.h"

@interface AvnWindow : NSWindow <AvnWindowProtocol, NSWindowDelegate, AvnAccessibility>
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
-(AvnView* _Nullable) view;
@end

@interface AvnPanel : NSPanel <AvnWindowProtocol, NSWindowDelegate, AvnAccessibility>
-(AvnPanel* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
@end
