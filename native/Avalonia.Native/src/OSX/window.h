#ifndef window_h
#define window_h

#import "avalonia-native.h"

@class AvnMenu;

class WindowBaseImpl;

@interface AutoFitContentView : NSView
-(AutoFitContentView* _Nonnull) initWithContent: (NSView* _Nonnull) content;
-(void) ShowTitleBar: (bool) show;
-(void) SetTitleBarHeightHint: (double) height;

-(void) ShowBlur: (bool) show;
@end

@interface AvnWindow : NSWindow <NSWindowDelegate>
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent;
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
