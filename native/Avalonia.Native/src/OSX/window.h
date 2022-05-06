#ifndef window_h
#define window_h

#import "avalonia-native.h"
#import "WindowProtocol.h"

@class AvnMenu;

class WindowBaseImpl;

@interface AvnWindow : NSWindow <AvnWindowProtocol, NSWindowDelegate>
-(AvnWindow* _Nonnull) initWithParent: (WindowBaseImpl* _Nonnull) parent contentRect: (NSRect)contentRect styleMask: (NSWindowStyleMask)styleMask;
@end

#endif /* window_h */
