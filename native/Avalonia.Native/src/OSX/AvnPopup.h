//
//  AvnPopup.h
//  Avalonia.Native.OSX
//
//  Created by Benedikt Stebner on 06.05.24.
//  Copyright © 2024 Avalonia. All rights reserved.
//

#pragma once

#import <AppKit/AppKit.h>

@protocol AvnPopupProtocol
-(double) getExtendedTitleBarHeight;
-(bool) shouldTryToHandleEvents;
@end
