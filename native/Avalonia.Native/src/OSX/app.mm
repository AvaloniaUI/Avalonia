#include "common.h"
#include "AvnString.h"
#include "menu.h"
@interface AvnAppDelegate : NSObject<NSApplicationDelegate>
-(AvnAppDelegate* _Nonnull) initWithEvents: (const ComPtr<IAvnApplicationEvents>&) events;
-(void) releaseEvents;
@end

NSApplicationActivationPolicy AvnDesiredActivationPolicy = NSApplicationActivationPolicyRegular;
static NSMenu* s_dockMenu = nil;

static bool IsOSShutdown()
{
    auto evt = [[NSAppleEventManager sharedAppleEventManager] currentAppleEvent];
    if ([evt eventClass] != kCoreEventClass || [evt eventID] != kAEQuitApplication)
        return false;

    auto reason = [evt paramDescriptorForKeyword:kAEQuitReason];
    if (reason == nil)
        reason = [evt attributeDescriptorForKeyword:kAEQuitReason];

    auto reasonCode = [reason enumCodeValue];
    if (reasonCode == 0)
        reasonCode = [reason typeCodeValue];

    switch (reasonCode)
    {
        case kAELogOut:
        case kAEReallyLogOut:
        case kAEShowRestartDialog:
        case kAERestart:
        case kAEShowShutdownDialog:
        case kAEShutDown:
            return true;
        default:
            return false;
    }
}

@implementation AvnAppDelegate
// Not released at exit: .NET is already shut down by then
ComStaticPtr<IAvnApplicationEvents> _events;

- (AvnAppDelegate *)initWithEvents:(const ComPtr<IAvnApplicationEvents>&)events
{
    _events.set(events);
    return self;
}

- (void)releaseEvents
{
    _events.reset();
}

- (void)applicationWillFinishLaunching:(NSNotification *)notification
{
    if([[NSApplication sharedApplication] activationPolicy] != AvnDesiredActivationPolicy)
    {
        for (NSRunningApplication * app in [NSRunningApplication runningApplicationsWithBundleIdentifier:@"com.apple.dock"]) {
            [app activateWithOptions:NSApplicationActivateIgnoringOtherApps];
            break;
        }
        
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

- (void) applicationDidBecomeActive:(NSNotification *) notification
{
    _events->OnActivate();
}

- (void) applicationDidResignActive:(NSNotification *) notification
{
    _events->OnDeactivate();
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
    switch (_events->TryShutdown(IsOSShutdown()))
    {
        case ShutdownReplyCancel:
            return NSTerminateCancel;
            
        // The managed dispatcher loop is exiting: let it handle the termination instead.
        case ShutdownReplyDeferToManagedLoop:
            return NSTerminateCancel;

        case ShutdownReplyTerminateNow:
            return NSTerminateNow;

        // Shouldn't happen
        default:
            return NSTerminateNow;
    }
}

- (void)applicationWillTerminate:(NSNotification *)notification
{
    if (!_events)
        return;
    
    // The process is about to exit() so this is the last point where managed code can still safely be called.
    // Keep the application events object alive for the duration of the call, it's about to be released by the managed side.
    ComPtr<IAvnApplicationEvents> events(_events);
    events->OnTerminating();
}

- (NSMenu *)applicationDockMenu:(NSApplication *)sender
{
    return s_dockMenu;
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

// NSApplication.delegate is weak
static AvnAppDelegate* s_appDelegate;

extern void InitializeAvnApp(const ComPtr<IAvnApplicationEvents>& events, bool disableAppDelegate)
{
    if(!disableAppDelegate)
    {
        NSApplication* app = [AvnApplication sharedApplication];
        s_appDelegate = [[AvnAppDelegate alloc] initWithEvents:events];
        [app setDelegate:s_appDelegate];
    }
}

extern void ReleaseAvnAppEvents()
{
    if (s_appDelegate == nil)
        return;
    NSApplication* app = [AvnApplication sharedApplication];
    if ([app delegate] == s_appDelegate)
        [app setDelegate:nil];
    [s_appDelegate releaseEvents];
    s_appDelegate = nil;
}

HRESULT AvnApplicationCommands::UnhideApp()
{
    [[NSApplication sharedApplication] unhide:[NSApp delegate]];
    return S_OK;
}

HRESULT AvnApplicationCommands::HideApp()
{
    [[NSApplication sharedApplication] hide:[NSApp delegate]];
    return S_OK;
}

HRESULT AvnApplicationCommands::ShowAll()
{
    [[NSApplication sharedApplication] unhideAllApplications:[NSApp delegate]];
    return S_OK;
}

HRESULT AvnApplicationCommands::HideOthers()
{
    [[NSApplication sharedApplication] hideOtherApplications:[NSApp delegate]];
    return S_OK;
}


extern ComPtr<IAvnApplicationCommands> CreateApplicationCommands()
{
    return comnew<AvnApplicationCommands>();
}

extern void SetDockMenu(NSMenu* menu)
{
    s_dockMenu = menu;
}
