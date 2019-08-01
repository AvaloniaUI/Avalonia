//
//  menu.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 01/08/2019.
//  Copyright © 2019 Avalonia. All rights reserved.
//

//#ifndef menu_h
//#define menu_h

@interface AvnMenu : NSMenu // for some reason it doesnt detect nsmenu here but compiler doesnt complain
+(void) myaction;
@end

@interface AvnMenuItem : NSMenuItem
+(void) myaction; // added myaction method
@end

//#endif /* menu_h */
