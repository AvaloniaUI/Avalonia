#include "common.h"
#include "AvnString.h"
@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
-(AvnAppDelegate* _Nonnull) initWithEvents: (IAvnApplicationEvents* _Nonnull) events;
-(void) releaseEvents;
@end

NSApplicationActivationPolicy AvnDesiredActivationPolicy = NSApplicationActivationPolicyRegular;

@implementation AvnAppDelegate
ComPtr<IAvnApplicationEvents> _events;

- (AvnAppDelegate *)initWithEvents:(IAvnApplicationEvents *)events
{
    _events = events;
    return self;
}

- (void)releaseEvents
{
    _events = nil;
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

-(BOOL)applicationShouldHandleReopen:(NSApplication *)sender hasVisibleWindows:(BOOL)flag
{
    _events->OnReopen();
    return YES;
}

- (void)applicationDidHide:(NSNotification *)notification
{
    _events->OnHide();
}

- (void)applicationDidUnhide:(NSNotification *)notification
{
    _events->OnUnhide();
}

- (void)application:(NSApplication *)sender openFiles:(NSArray<NSString *> *)filenames
{
    auto array = CreateAvnStringArray(filenames);
    
    _events->FilesOpened(array);
}

- (void)application:(NSApplication *)application openURLs:(NSArray<NSURL *> *)urls
{
    auto array = CreateAvnStringArray(urls);
    
    _events->UrlsOpened(array);
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

extern void InitializeAvnApp(IAvnApplicationEvents* events, bool disableAppDelegate)
{
    if(!disableAppDelegate)
    {
        NSApplication* app = [AvnApplication sharedApplication];
        id delegate = [[AvnAppDelegate alloc] initWithEvents:events];
        [app setDelegate:delegate];
    }
}

extern void ReleaseAvnAppEvents()
{
    NSApplication* app = [AvnApplication sharedApplication];
    id delegate = [app delegate];
    if ([delegate isMemberOfClass:[AvnAppDelegate class]])
    {
        AvnAppDelegate* avnDelegate = delegate;
        [avnDelegate releaseEvents];
        [app setDelegate:nil];
    }
}

HRESULT AvnApplicationCommands::UnhideApp()
{
    START_COM_CALL;
    [[NSApplication sharedApplication] unhide:[NSApp delegate]];
    return S_OK;
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
