#include "common.h"
#include "AvnString.h"
@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
-(AvnAppDelegate* _Nonnull) initWithEvents: (IAvnApplicationEvents* _Nonnull) events;
@end

NSApplicationActivationPolicy AvnDesiredActivationPolicy = NSApplicationActivationPolicyRegular;

@implementation AvnAppDelegate
ComPtr<IAvnApplicationEvents> _events;

- (AvnAppDelegate *)initWithEvents:(IAvnApplicationEvents *)events
{
    _events = events;
    return self;
}

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

- (void)application:(NSApplication *)sender openFiles:(NSArray<NSString *> *)filenames
{
    auto array = CreateAvnStringArray(filenames);
    
    _events->FilesOpened(array);
}

- (void)application:(NSApplication *)application openURLs:(NSArray<NSURL *> *)urls
{
    auto array = CreateAvnStringArray(urls);
    
    _events->FilesOpened(array);
}

- (NSApplicationTerminateReply)applicationShouldTerminate:(NSApplication *)sender
{
    return _events->TryShutdown() ? NSTerminateNow : NSTerminateCancel;
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
        if ([event type] == NSEventTypeKeyUp && ([event modifierFlags] & NSEventModifierFlagCommand))
        {
            [[self keyWindow] sendEvent:event];
        }
        
    } @finally {
        _isHandlingSendEvent = oldHandling;
    }
}

// This is needed for certain embedded controls DO NOT REMOVE..
- (BOOL) isHandlingSendEvent
{
    return _isHandlingSendEvent;
}

- (void)setHandlingSendEvent:(BOOL)handlingSendEvent
{
    _isHandlingSendEvent = handlingSendEvent;
}
@end

extern void InitializeAvnApp(IAvnApplicationEvents* events)
{
    NSApplication* app = [AvnApplication sharedApplication];
    id delegate = [[AvnAppDelegate alloc] initWithEvents:events];
    [app setDelegate:delegate];
}

HRESULT AvnApplicationCommands::HideApp()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] hide:[NSApp delegate]];
    return S_OK;
}

HRESULT AvnApplicationCommands::ShowAll()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] unhideAllApplications:[NSApp delegate]];
    return S_OK;
}

HRESULT AvnApplicationCommands::HideOthers()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] hideOtherApplications:[NSApp delegate]];
    return S_OK;
}


extern IAvnApplicationCommands* CreateApplicationCommands()
{
    return new AvnApplicationCommands();
}
