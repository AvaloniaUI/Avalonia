#import <AppKit/AppKit.h>
#include "AvnPopup.h"
#import "WindowInterfaces.h"

@implementation AvnPopup
{
    ComPtr<WindowBaseImpl> _parent;
}

- (AvnPopup * _Nonnull)initWithWindowImpl:(WindowBaseImpl * _Nonnull)windowImpl contentRect:(NSRect)contentRect {
    
    self = [super initWithContentRect:contentRect styleMask: NSWindowStyleMaskBorderless backing:NSBackingStoreBuffered defer:false];
    
    return self;
}

- (double)getExtendedTitleBarHeight { 
    return 0;
}

- (bool)shouldTryToHandleEvents { 
    return YES;
}

@end
