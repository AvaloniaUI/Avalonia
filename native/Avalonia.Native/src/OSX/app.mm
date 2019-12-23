#include "common.h"
#include <Cocoa/Cocoa.h>

@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
@end

extern NSApplicationActivationPolicy AvnDesiredActivationPolicy = NSApplicationActivationPolicyRegular;

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
    }
}

- (void)applicationDidFinishLaunching:(NSNotification *)notification
{
    [[NSRunningApplication currentApplication] activateWithOptions:NSApplicationActivateIgnoringOtherApps];
}

- (void)userNotificationCenter:(NSUserNotificationCenter *)center
       didActivateNotification:(NSUserNotification *)notification{
    
    if (notification.activationType != NSUserNotificationActivationTypeContentsClicked &&
        notification.activationType != NSUserNotificationActivationTypeActionButtonClicked) {
        return;
    }
    
    NSValue* actionCallbackValue = (NSValue*) notification.userInfo[@"actionCallback"];
    
    if (actionCallbackValue.pointerValue) {
        AvnNotificationActionCallback actionCallback =
            (AvnNotificationActionCallback) actionCallbackValue.pointerValue;
        actionCallback();
    }
    
    //TODO: I can't really find a proper event for closing.
    NSValue* closeCallbackValue = (NSValue*) notification.userInfo[@"closeCallback"];
    
    if (closeCallbackValue.pointerValue) {
        AvnNotificationActionCallback closeCallback =
            (AvnNotificationCloseCallback) closeCallbackValue.pointerValue;
        closeCallback();
    }
}

@end

extern void InitializeAvnApp()
{
    NSApplication* app = [NSApplication sharedApplication];
    id delegate = [AvnAppDelegate new];
    [app setDelegate:delegate];
}
