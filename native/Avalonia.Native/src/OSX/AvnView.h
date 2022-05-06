//
// Created by Dan Walmsley on 05/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#import <Foundation/Foundation.h>


#import <Foundation/Foundation.h>
#import <AppKit/AppKit.h>
#include "window.h"
#import "comimpl.h"
#import "common.h"
#import "WindowImpl.h"
#import "KeyTransform.h"

@class AvnAccessibilityElement;

@interface AvnView : NSView<NSTextInputClient, NSDraggingDestination>
-(AvnView* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
-(NSEvent* _Nonnull) lastMouseDownEvent;
-(AvnPoint) translateLocalPoint:(AvnPoint)pt;
-(void) setSwRenderedFrame: (AvnFramebuffer* _Nonnull) fb dispose: (IUnknown* _Nonnull) dispose;
-(void) onClosed;

-(AvnPlatformResizeReason) getResizeReason;
-(void) setResizeReason:(AvnPlatformResizeReason)reason;
+ (AvnPoint)toAvnPoint:(CGPoint)p;
@end