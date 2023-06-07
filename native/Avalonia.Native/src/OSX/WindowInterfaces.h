//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include "WindowProtocol.h"
#include "WindowBaseImpl.h"

@interface AvnWindow : NSWindow <AvnWindowProtocol, NSWindowDelegate>
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
@end

@interface AvnPanel : NSPanel <AvnWindowProtocol, NSWindowDelegate>
-(AvnPanel* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
@end