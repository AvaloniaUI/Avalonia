#include "common.h"
#include "AvnString.h"
#include "SandboxBookmark.h"
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
    if(GetIsAppStoreSandboxEnabled())
    {
        for(int c = 0; c < [filenames count]; c++)
        {
            NSString* str = [filenames objectAtIndex:c];
            NSURL* url = [NSURL URLWithString:str];
            IAvnSandboxBookmark* bookmark = CreateSandboxBookmark(url);
            _events->SandboxBookmarkAdded(bookmark);
        }
    }
    
    auto array = CreateAvnStringArray(filenames);
    
    _events->FilesOpened(array);
}

- (void)application:(NSApplication *)application openURLs:(NSArray<NSURL *> *)urls
{
    if(GetIsAppStoreSandboxEnabled())
    {
        for(int c = 0; c < [urls count]; c++)
        {
            NSURL* url = [urls objectAtIndex:c];
            IAvnSandboxBookmark* bookmark = CreateSandboxBookmark(url);
            _events->SandboxBookmarkAdded(bookmark);
        }
    }
    
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

extern void InitializeAvnApp(IAvnApplicationEvents* events)
{
    NSApplication* app = [AvnApplication sharedApplication];
    id delegate = [[AvnAppDelegate alloc] initWithEvents:events];
    [app setDelegate:delegate];
}
