#ifndef window_h
#define window_h

#import "avalonia-native.h"

@class AvnMenu;

class WindowBaseImpl;

@interface AvnWindow : NSWindow <NSWindowDelegate>
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
-(void) setCanBecomeKeyAndMain;
-(void) pollModalSession: (NSModalSession _Nonnull) session;
-(void) restoreParentWindow;
-(bool) shouldTryToHandleEvents;
-(void) setEnabled: (bool) enable;
-(void) showAppMenuOnly;
-(void) showWindowMenuWithAppMenu;
-(void) applyMenu:(AvnMenu* _Nullable)menu;

-(double) getExtendedTitleBarHeight;
-(void) setIsExtended:(bool)value;
-(bool) isDialog;
@end

#endif /* window_h */
