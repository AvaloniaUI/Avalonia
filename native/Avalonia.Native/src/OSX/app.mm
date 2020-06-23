#include "common.h"
@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
@end

NSApplicationActivationPolicy AvnDesiredActivationPolicy = NSApplicationActivationPolicyRegular;

@implementation AvnAppDelegate
- (void)applicationWillFinishLaunching:(NSNotification *)notification
{
    if([[NSApplication sharedApplication] activationPolicy] != AvnDesiredActivationPolicy)
    {
        for (NSRunningApplication * app in [NSRunningApplication runningApplicationsWithBundleIdentifier:@"com.apple.dock"]) {
            [app activateWithOptions:NSApplicationActivateIgnoringOtherApps];
            break;
        }
        
        [[NSApplication sharedApplication] setActivationPolicy: AvnDesiredActivationPolicy];
        
        [[NSUserDefaults standardUserDefaults] setBool:NO forKey:@"NSFullScreenMenuItemEverywhere"];
        
        [[NSApplication sharedApplication] setHelpMenu: [[NSMenu new] initWithTitle:@""]];
    }
}

- (void)applicationDidFinishLaunching:(NSNotification *)notification
{
    [[NSRunningApplication currentApplication] activateWithOptions:NSApplicationActivateIgnoringOtherApps];
}

@end

@interface AvnApplication : NSApplication


@end

@implementation AvnApplication
{
    BOOL _isHandlingSendEvent;
}

- (void)sendEvent:(NSEvent *)event
{
    bool oldHandling = _isHandlingSendEvent;
    _isHandlingSendEvent = true;
    @try {
        [super sendEvent: event];
    } @finally {
        _isHandlingSendEvent = oldHandling;
    }
}

// This is needed for certain embedded controls
- (BOOL) isHandlingSendEvent
{
    return _isHandlingSendEvent;
}

- (void)setHandlingSendEvent:(BOOL)handlingSendEvent
{
    _isHandlingSendEvent = handlingSendEvent;
}

@end

extern void InitializeAvnApp()
{
    NSApplication* app = [AvnApplication sharedApplication];
    id delegate = [AvnAppDelegate new];
    [app setDelegate:delegate];
}
