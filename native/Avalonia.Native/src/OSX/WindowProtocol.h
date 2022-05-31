//
// Created by Dan Walmsley on 06/05/2022.
// Copyright (c) 2022 Avalonia. All rights reserved.
//

#pragma once

#import <AppKit/AppKit.h>

@class AvnMenu;

@protocol AvnWindowProtocol
-(void) pollModalSession: (NSModalSession _Nonnull) session;
-(bool) shouldTryToHandleEvents;
-(void) setEnabled: (bool) enable;
-(void) showAppMenuOnly;
-(void) showWindowMenuWithAppMenu;
-(void) applyMenu:(AvnMenu* _Nullable)menu;

-(double) getExtendedTitleBarHeight;
-(void) setIsExtended:(bool)value;
-(void) disconnectParent;
-(bool) isDialog;

-(void) setCanBecomeKeyWindow:(bool)value;
@end
