#include "common.h"
@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
@end

@implementation AvnAppDelegate
- (void)applicationWillFinishLaunching:(NSNotification *)notification
{
    
}

- (void)applicationDidFinishLaunching:(NSNotification *)notification
{
    [NSApp activateIgnoringOtherApps:true];
}

@end

extern void InitializeAvnApp()
{
    NSApplication* app = [NSApplication sharedApplication];
    id delegate = [AvnAppDelegate new];
    [app setDelegate:delegate];
    
}
