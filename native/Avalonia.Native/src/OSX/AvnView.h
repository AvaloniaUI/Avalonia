//
// Created by Dan Walmsley on 05/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//
#pragma once
#import <Foundation/Foundation.h>

#import <AppKit/AppKit.h>
#include "common.h"
#include "WindowImpl.h"
#include "KeyTransform.h"

@class AvnAccessibilityElement;

@interface AvnView : NSView<NSTextInputClient, NSDraggingDestination, AvnTextInputMethodDelegate>
-(AvnView* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(NSEvent* _Nonnull) lastMouseDownEvent;
-(AvnPoint) translateLocalPoint:(AvnPoint)pt;
-(void) setSwRenderedFrame: (AvnFramebuffer* _Nonnull) fb dispose: (IUnknown* _Nonnull) dispose;
-(void) onClosed;

-(AvnPlatformResizeReason) getResizeReason;
-(void) setResizeReason:(AvnPlatformResizeReason)reason;
+ (AvnPoint)toAvnPoint:(CGPoint)p;
@end
