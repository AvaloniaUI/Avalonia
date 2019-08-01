//
//  menu.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 01/08/2019.
//  Copyright © 2019 Avalonia. All rights reserved.
//

#ifndef menu_h
#define menu_h

#include "common.h"

@interface AvnMenu : NSMenu
+(void) myaction;
@end

@interface AvnMenuItem : NSMenuItem
+(void) myaction;
@end

#endif /* menu_h */
