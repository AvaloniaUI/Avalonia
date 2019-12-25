#include "common.h"
#include <Cocoa/Cocoa.h>
#import <objc/runtime.h>

@implementation NSBundle (FakeBundleIdentifier)

- (NSString *)__bundleIdentifier;
{
    if (self == [NSBundle mainBundle]) {
        return @"com.avalonia.native.osx";
    } else {
        return [self __bundleIdentifier];
    }
}

@end

//It is required to install a bundle identifier in order to use the notification center.
//Otherwise [NSUserNotificationCenter defaultUserNotificationCenter] will be nil.
//https://github.com/munki/munki/blob/master/code/apps/munki-notifier/munki-notifier/AppDelegate.m
static BOOL InstallFakeBundleIdentifierHook()
{
    Class classImpl = objc_getClass("NSBundle");
    if (classImpl) {
        method_exchangeImplementations(class_getInstanceMethod(classImpl, @selector(bundleIdentifier)),
                                       class_getInstanceMethod(classImpl, @selector(__bundleIdentifier)));
        return YES;
    }
    return NO;
}

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
    
    @autoreleasepool {
        InstallFakeBundleIdentifierHook();
    }
}

@end

extern void InitializeAvnApp()
{
    NSApplication* app = [NSApplication sharedApplication];
    id delegate = [AvnAppDelegate new];
    [app setDelegate:delegate];
}
