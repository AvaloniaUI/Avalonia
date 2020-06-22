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

extern void InitializeAvnApp()
{
    NSApplication* app = [NSApplication sharedApplication];
    id delegate = [AvnAppDelegate new];
    [app setDelegate:delegate];
}
