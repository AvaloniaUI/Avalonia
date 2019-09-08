#include "common.h"
@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
@end
extern NSApplicationActivationPolicy AvnDesiredActivationPolicy = NSApplicationActivationPolicyRegular;
@implementation AvnAppDelegate
- (void)applicationWillFinishLaunching:(NSNotification *)notification
{
    [[NSApplication sharedApplication] setActivationPolicy: AvnDesiredActivationPolicy];
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
